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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Elysium;
using GalaSoft.MvvmLight.Messaging;
using HueMove.Properties;
using Q42.HueApi;

namespace HueMove
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
		}

		public static LightTimer Timer;

		public static readonly ICommand GetUp = new GettingUpCommand();
		public static readonly ICommand Snooze = new SnoozeCommand();

		private readonly Version Windows8 = new Version (6, 2);

		protected override async void OnStartup (StartupEventArgs e)
		{
			base.OnStartup (e);

			SolidColorBrush accentBrush = AccentBrushes.Blue;
			/*if (Environment.OSVersion.Version.CompareTo (Windows8) >= 0) {
				Color color = ColorFunctions.GetImmersiveColor (ImmersiveColors.ImmersiveControlDarkButtonBackgroundRest);
				accentBrush = new SolidColorBrush (color);
			}*/

			Current.Apply (Theme.Dark, accentBrush, Brushes.White);

			Messenger.Default.Register<BridgeSelectedMessage> (this, OnBridgeSelected);
			Messenger.Default.Register<MoveMessage> (this, OnMoveMessage);

			if (String.IsNullOrWhiteSpace (Settings.Default.Bridge)) {
				SelectBridgeWindow select = new SelectBridgeWindow();
				select.Show();
			} else {
				OnBridgeSelected (new BridgeSelectedMessage (Settings.Default.Bridge));
			}
		}

		private const string AppName = "HueMove";
		private const string AppUsername = "HueMoveUser";
		private async void OnBridgeSelected (BridgeSelectedMessage msg)
		{
			HueClient client;
			if (String.IsNullOrWhiteSpace (Settings.Default.BridgeUsername) || msg.Bridge != Settings.Default.Bridge) {
				Settings.Default.Bridge = msg.Bridge;
				Settings.Default.BridgeUsername = AppUsername;

				client = new HueClient (msg.Bridge);

				bool registered = !await client.RegisterAsync (AppName, AppUsername);
				if (!registered) {
					ConnectWindow connect = new ConnectWindow();
					connect.ShowToast();

					while (!await client.RegisterAsync (AppName, AppUsername))
						;

					connect.Close();
				}

				Settings.Default.Save();
			} else
				client = new HueClient (msg.Bridge, Settings.Default.BridgeUsername);

			Timer = new LightTimer (client);
			Timer.Start();
		}

		private void OnMoveMessage (MoveMessage moveMessage)
		{
			Dispatcher.BeginInvoke ((Action)(() => {
				NoticeWindow window = new NoticeWindow();
				window.ShowToast();
			}));
		}
	}
}
