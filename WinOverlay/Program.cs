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
            
            //if (args.Length == 0)
            //{
            //    Application.Run(new NoSignalForm());
            //}
            //else
            //{
            //}
            Application.Run(new OverlayManager());
        }
    }
}