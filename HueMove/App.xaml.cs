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
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using Elysium;
using GalaSoft.MvvmLight.Messaging;
using HueMove.Properties;
using Q42.HueApi;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;

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

		public static HueClient Client
		{
			get;
			private set;
		}

		public static readonly ICommand GetUp = new GettingUpCommand();
		public static readonly ICommand Snooze = new SnoozeCommand();
		public static readonly ICommand Back = new BackCommand();

		private static readonly Version Windows8 = new Version (6, 2);

		protected override async void OnStartup (StartupEventArgs e)
		{
			base.OnStartup (e);

			SolidColorBrush textBrush = (Settings.Default.Theme == Theme.Dark) ? Brushes.White : Brushes.Black;
			SolidColorBrush accentBrush = AccentBrushes.Blue;
			if (Settings.Default.EnableImmersiveColors && Environment.OSVersion.Version.CompareTo (Windows8) >= 0) {
				var backgroundColor = (Settings.Default.Theme == Theme.Dark)
					? ImmersiveColors.ImmersiveSaturatedSelectionBackground
					: ImmersiveColors.ImmersiveLightSelectionBackground;

				Color color = ColorFunctions.GetImmersiveColor (backgroundColor);
				accentBrush = new SolidColorBrush (color);

				var textColor = (Settings.Default.Theme == Theme.Dark)
					? ImmersiveColors.ImmersiveSaturatedSelectionPrimaryText
					: ImmersiveColors.ImmersiveLightSelectionPrimaryText;

				color = ColorFunctions.GetImmersiveColor (textColor);
				textBrush = new SolidColorBrush (color);
			}

			Current.Apply (Settings.Default.Theme, accentBrush, textBrush);

			Messenger.Default.Register<BridgeSelectedMessage> (this, OnBridgeSelected);
			Messenger.Default.Register<MoveMessage> (this, OnMoveMessage);

			SetupTrayIcon();

			if (String.IsNullOrWhiteSpace (Settings.Default.Bridge)) {
				SelectBridgeWindow select = new SelectBridgeWindow();
				select.Show();
			} else {
				OnBridgeSelected (new BridgeSelectedMessage (Settings.Default.Bridge));
			}
		}

		private void SetupTrayIcon()
		{
			var contextMenu = new ContextMenu();

			this.getUpMenuItem = new MenuItem ("Getting up") {
				Enabled = false
			};

			this.getUpMenuItem.Click += (sender, args) => {
				App.Timer.GetUp (timed: false);

				var goneWindow = new GoneWindow();
				goneWindow.ShowToast();
			};

			contextMenu.MenuItems.Add (getUpMenuItem);

			contextMenu.MenuItems.Add (new MenuItem ("-"));

			var exit = new MenuItem ("E&xit");
			exit.Click += (o, e) => Application.Current.Shutdown();
			contextMenu.MenuItems.Add (exit);

			this.trayIcon = new NotifyIcon {
				ContextMenu = contextMenu,
				Icon = HueMove.Properties.Resources.TrayIcon
			};

			this.trayIcon.Visible = true;
		}

		private const string AppName = "HueMove";
		private const string AppUsername = "HueMoveUser";

		private NotifyIcon trayIcon;
		private MenuItem getUpMenuItem;

		private async void OnBridgeSelected (BridgeSelectedMessage msg)
		{
			if (String.IsNullOrWhiteSpace (Settings.Default.BridgeUsername) || msg.Bridge != Settings.Default.Bridge) {
				Settings.Default.Bridge = msg.Bridge;
				Settings.Default.BridgeUsername = AppUsername;

				Client = new HueClient (msg.Bridge);

				bool registered = !await Client.RegisterAsync (AppName, AppUsername);
				if (!registered) {
					ConnectWindow connect = new ConnectWindow();
					connect.ShowToast();

					while (!await Client.RegisterAsync (AppName, AppUsername))
						;

					connect.Close();
				}

				Settings.Default.Save();
			} else
				Client = new HueClient (msg.Bridge, Settings.Default.BridgeUsername);

			if (Settings.Default.Lights == null) {
				SelectLightsWindow select = new SelectLightsWindow();
				select.ShowDialog();
			}

			this.getUpMenuItem.Enabled = true;

			Timer = new LightTimer (Client);
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
