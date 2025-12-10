using System;
using System.Windows;

namespace WinOverlay
{
    class ProgramWpf
    {
        [STAThread]
        static void Main(string[] args)
        {
            int cameraId = 0;
            
            // Parse command line arguments
            if (args.Length > 0 && int.TryParse(args[0], out int parsedId))
            {
                cameraId = parsedId;
            }

            Console.WriteLine($"Starting WPF Overlay Window for Camera {cameraId}");

            // Create WPF application
            var app = new Application();
            
            // Create and show the WPF window
            var window = new WoWpf(cameraId);
            window.Show();

            // Run the WPF message loop
            app.Run(window);
        }
    }
}
