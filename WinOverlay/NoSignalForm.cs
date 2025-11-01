using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinOverlay
{
    public class NoSignalForm : Form
    {
        private Timer closeTimer;
        private Timer countdownTimer;
        private int countdown = 10;

        public NoSignalForm()
        {
            Text = "WinOverlay";
            Size = new Size(100, 100);
            BackColor = Color.Black;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            
            closeTimer = new Timer();
            closeTimer.Interval = 10000;
            closeTimer.Tick += (s, e) => Close();
            closeTimer.Start();
            
            countdownTimer = new Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += (s, e) => { countdown--; Invalidate(); };
            countdownTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var brush = new SolidBrush(Color.White))
            using (var font = new Font("Arial", 8))
            {
                var noSignalSize = e.Graphics.MeasureString("No Signal", font);
                var countdownText = countdown.ToString();
                var countdownSize = e.Graphics.MeasureString(countdownText, font);
                
                var clientArea = ClientRectangle;
                var noSignalX = (clientArea.Width - noSignalSize.Width) / 2;
                var noSignalY = (clientArea.Height - noSignalSize.Height - countdownSize.Height) / 2;
                var countdownX = (clientArea.Width - countdownSize.Width) / 2;
                var countdownY = noSignalY + noSignalSize.Height;
                
                e.Graphics.DrawString("No Signal", font, brush, noSignalX, noSignalY);
                e.Graphics.DrawString(countdownText, font, brush, countdownX, countdownY);
            }
        }
    }
}