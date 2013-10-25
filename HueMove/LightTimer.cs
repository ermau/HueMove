//
// Author:
//   Eric Maupin <me@ermau.com>
//
// Copyright (c) 2013 Eric Maupin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using HueMove.Properties;
using Q42.HueApi;

namespace HueMove
{
	public class LightTimer
	{
		public LightTimer (HueClient client)
		{
			if (client == null)
				throw new ArgumentNullException ("client");

			this.client = client;
		}

		public void Start()
		{
			if (this.isRunning)
				return;

			this.isRunning = true;
			Thread thread = new Thread (Runner);
			thread.Start();
		}

		public void Stop()
		{
			this.isRunning = false;
			this.snooze = TimeSpan.FromSeconds (0);
			this.onBreak = false;
			this.wait.Set();
		}

		public void GetUp (bool timed)
		{
			this.snooze = TimeSpan.FromSeconds (0);
			this.onBreak = true;
			this.timedBreak = timed;

			if (timed)
				this.wait.Set();
		}

		public void Snooze()
		{
			snooze += Settings.Default.SnoozeTime;
			this.wait.Set();
		}

		public void Back()
		{
			if (!this.onBreak)
				return;

			this.lastMoved = DateTime.Now;
			this.wait.Set();
		}

		private readonly HueClient client;
		private volatile bool isRunning = false;
		private volatile bool timedBreak;
		private volatile bool onBreak;
		private DateTime lastMoved = DateTime.Now;
		private TimeSpan snooze = TimeSpan.FromSeconds (0);

		private readonly AutoResetEvent wait = new AutoResetEvent (false);

		TimeSpan GetBreakTime()
		{
			TimeSpan sinceLastMoved = DateTime.Now - lastMoved;
			return TimeSpan.FromMinutes (sinceLastMoved.TotalMinutes * Settings.Default.BreakRatio);
		}

		private Light[] lastLights;
		private async Task AlertLightsAsync()
		{
			this.lastLights = (await this.client.GetLightsAsync()).ToArray();

			LightCommand cmd = new LightCommand {
				Alert = Alert.Once,
				On = true,
				Brightness = 255,
			};
			cmd = cmd.SetColor (255, 0, 0);

			await this.client.SendCommandAsync (cmd, this.lastLights.Where (l => l.State.On).Select (l => l.Id));
		}

		private async Task RestoreLightsAsync (int seconds = 5)
		{
			foreach (Light light in this.lastLights.Where (l => l.State.On)) {
				var cmd = new LightCommand {
					On = true,
					Brightness = light.State.Brightness,
					Alert = light.State.Alert,
					Effect = light.State.Effect,
					TransitionTime = TimeSpan.FromSeconds (seconds)
				};

				switch (light.State.ColorMode) {
					case "ct":
						cmd.SetColor (light.State.ColorTemperature);
						break;

					case "xy":
						cmd.SetColor (light.State.ColorCoordinates[0], light.State.ColorCoordinates[1]);
						break;

					case "hs":
						cmd.Saturation = light.State.Saturation;
						cmd.Hue = light.State.Hue;
						break;
				}

				await this.client.SendCommandAsync (cmd, new[] { light.Id });
			}
		}

		private void Runner()
		{
			while (this.isRunning) {
				if (!this.onBreak && (DateTime.Now - this.lastMoved) > (Settings.Default.WorkTime + this.snooze)) {
					Messenger.Default.Send (new MoveMessage());

					try {
						AlertLightsAsync().Wait();
					} catch (WebException) {
					}

					DateTime beforeAlert = DateTime.Now;
					wait.WaitOne(); // Wait for a user response
					this.snooze += DateTime.Now - beforeAlert; // we want to snooze from the point we hit the button

					if (this.onBreak) {
						TimeSpan breakLength = GetBreakTime();
						this.lastMoved = DateTime.Now + breakLength;
					} else {
						try {
							RestoreLightsAsync (0).Wait();
						} catch (WebException) {
						}
					}
				} else if (this.onBreak) {
					if (!timedBreak) {
						try {
							AlertLightsAsync().Wait();
						} catch (WebException) {
						}

						wait.WaitOne();
					}

					var now = DateTime.Now;
					if (now >= this.lastMoved) {
						try {
							RestoreLightsAsync (0).Wait();
						} catch (WebException) {
						}
					}

					this.onBreak = false;
					this.snooze = TimeSpan.FromSeconds (0);
				}

				Thread.Sleep (1000);
			}
		}
	}
}