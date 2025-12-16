using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WinOverlay;

namespace WinOverlayV2
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// Background service application for WinOverlay
	/// </summary>
	public partial class App : Application
	{
		private WOService woService;
		private SystemTrayManager trayManager;

		private void OnStartup(object sender, StartupEventArgs e)
		{
			// Initialize WOService (background service)
			woService = new WOService();

			// Initialize system tray
			trayManager = new SystemTrayManager(woService);

			// Show startup notification
			trayManager.ShowNotification(
				"WinOverlay Service",
				"Service started. Waiting for Unity3D connection..."
			);

			// Note: No window is shown at startup
			// Windows will be created dynamically when Unity sends RegisterCamera command
		}

		private void OnExit(object sender, ExitEventArgs e)
		{
			// Clean up resources
			trayManager?.Dispose();
			woService?.Dispose();
		}
	}
}
