using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using WinOverlay;

namespace WinOverlayV2
{
	/// <summary>
	/// Manages the system tray icon and context menu for the application using pure WPF.
	/// Uses Hardcodet.NotifyIcon.Wpf library.
	/// </summary>
	public class SystemTrayManager : IDisposable
	{
		private TaskbarIcon taskbarIcon;
		private WOService service;
		private bool isDisposed = false;

		public SystemTrayManager(WOService service)
		{
			this.service = service ?? throw new ArgumentNullException(nameof(service));
			InitializeTrayIcon();
		}

		private void InitializeTrayIcon()
		{
			taskbarIcon = new TaskbarIcon
			{
				// TODO: Replace with actual icon file
				// IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/TrayIcon.ico")),
				ToolTipText = "WinOverlay Service - Running"
			};

			// Create WPF context menu
			var contextMenu = new ContextMenu();
			
			// Status item
			var statusItem = new MenuItem
			{
				Header = "WinOverlay Service",
				IsEnabled = false
			};
			contextMenu.Items.Add(statusItem);
			
			contextMenu.Items.Add(new Separator());
			
			// Exit item
			var exitItem = new MenuItem
			{
				Header = "Exit"
			};
			exitItem.Click += OnExit;
			contextMenu.Items.Add(exitItem);

			taskbarIcon.ContextMenu = contextMenu;
			
			// Double-click to show status
			taskbarIcon.TrayMouseDoubleClick += OnTrayIconDoubleClick;
		}

		private void OnTrayIconDoubleClick(object sender, RoutedEventArgs e)
		{
			// Show a status message using balloon tip
			ShowNotification(
				"WinOverlay Service",
				"Service is running in background.\nWaiting for Unity3D commands."
			);
		}

		private void OnExit(object sender, RoutedEventArgs e)
		{
			// Shutdown the application
			Application.Current?.Shutdown();
		}

		public void ShowNotification(string title, string message)
		{
			if (taskbarIcon != null && !isDisposed)
			{
				taskbarIcon.ShowBalloonTip(title, message, BalloonIcon.Info);
			}
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				isDisposed = true;
				
				if (taskbarIcon != null)
				{
					taskbarIcon.Dispose();
					taskbarIcon = null;
				}
			}
		}
	}
}
