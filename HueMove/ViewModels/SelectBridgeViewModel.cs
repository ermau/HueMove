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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Q42.HueApi;

namespace HueMove
{
	class SelectBridgeViewModel
		: ViewModelBase
	{
		public SelectBridgeViewModel()
		{
			this.timer = new Timer (OnTimer, SynchronizationContext.Current, 0, 100000);
			this.selectBridge = new RelayCommand<string> (SelectBridgeCore, CanSelectBridge);
		}

		public IEnumerable<string> Bridges
		{
			get { return this.bridges; }
		}

		public ICommand SelectBridge
		{
			get { return this.selectBridge; }
		}

		private readonly Timer timer;
		private readonly HttpBridgeLocator locator = new HttpBridgeLocator();
		private readonly ObservableCollection<string> bridges = new ObservableCollection<string>();
		private readonly RelayCommand<string> selectBridge;

		private bool CanSelectBridge (string s)
		{
			return (s != null);
		}

		private void SelectBridgeCore (string s)
		{
			MessengerInstance.Send (new BridgeSelectedMessage (s));
			this.timer.Dispose();
		}

		private async void OnTimer (object state)
		{
			var context = (SynchronizationContext)state;

			var queryedBridges = new HashSet<string> (await locator.LocateBridgesAsync (TimeSpan.FromSeconds (50)));
			
			context.Post (s => {
				var qb = (HashSet<string>)s;
				foreach (string bridge in qb) {
					if (!bridges.Contains (bridge))
						bridges.Add (bridge);
				}

				foreach (string bridge in bridges.ToArray()) {
					if (!qb.Contains (bridge))
						bridges.Remove (bridge);
				}
			}, queryedBridges);
		}
	}
}