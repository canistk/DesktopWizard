using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinOverlay
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            if (args.Length == 0)
            {
                Application.Run(new NoSignalForm());
            }
            else
            {
                Application.Run(new OverlayManager());
            }
        }

        private static OverlayConfig ParseArgs(string[] args)
        {
            var config = new OverlayConfig();
            int _x = 0, _y = 0, _w = 320, _h = 240;
            for (int i = 0; i < args.Length - 1; i++)
            {
                var key = args[i];
				var val = int.Parse(args[i + 1]);
				switch (key)
                {
                    case "-cameraId":
                        config.CameraId = args[i + 1];
                        break;
                    case "-x": _x = val; break;
                    case "-y": _y = val; break;
                    case "-w": _w = val; break;
                    case "-h": _h = val; break;
                }
			}
            config.Bounds = new Rectangle(_x, _y, _w, _h);
            
            return config;
        }
    }
}