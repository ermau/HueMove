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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HueMove.Properties;
using JetBrains.Annotations;
using Q42.HueApi;

namespace HueMove
{
	class SelectLightsViewModel
		: ViewModelBase
	{
		public SelectLightsViewModel ([NotNull] HueClient client)
		{
			if (client == null)
				throw new ArgumentNullException ("client");

			Accept = new RelayCommand(AcceptLights);
			this.client = client;
			OnTimer (null);
			//this.timer = new Timer (OnTimer, null, 0, 3000);
		}

		public ICommand Accept
		{
			get;
			private set;
		}

		public IEnumerable<LightViewModel> Lights
		{
			get { return this.lights; }
			private set { Set ("Lights", ref this.lights, value); }
		}

		private readonly HueClient client;
		private IEnumerable<LightViewModel> lights;
		private Timer timer;

		private void OnTimer (object state)
		{
			this.client.GetLightsAsync().ContinueWith (t => {
				Lights = t.Result.Select (l => new LightViewModel (l)).ToArray();
			}, TaskContinuationOptions.OnlyOnRanToCompletion);
		}

		private void AcceptLights()
		{
			Settings.Default.Lights = new StringCollection();
			foreach (LightViewModel vm in Lights.Where (l => l.IsSelected))
				Settings.Default.Lights.Add (vm.Light.Id);

			Settings.Default.Save();
		}
	}

	class LightViewModel
	{
		public LightViewModel (Light light)
		{
			this.light = light;
			IsSelected = true;
		}

		public Light Light
		{
			get { return this.light; }
		}

		public bool IsSelected
		{
			get;
			set;
		}

		private readonly Light light;
	}
}
