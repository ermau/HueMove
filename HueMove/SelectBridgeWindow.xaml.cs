﻿//
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
using GalaSoft.MvvmLight.Messaging;

namespace HueMove
{
	/// <summary>
	/// Interaction logic for SelectBridgeWindow.xaml
	/// </summary>
	public partial class SelectBridgeWindow : Elysium.Controls.Window
	{
		public SelectBridgeWindow()
		{
			InitializeComponent();
			
			Messenger.Default.Register<BridgeSelectedMessage> (this, msg => {
				this.closeApp = false;
				Close();
			});

			DataContext = new SelectBridgeViewModel();
		}

		private bool closeApp = true;

		protected override void OnClosed (EventArgs e)
		{
			base.OnClosed (e);
			if (this.closeApp)
				Application.Current.Shutdown();
		}
	}
}
