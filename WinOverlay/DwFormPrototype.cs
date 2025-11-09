using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinOverlay
{
    /// <summary>
    /// Minimal prototype Form to test basic Windows Form creation and display.
    /// This is a simplified version without shared memory, GPU funnels, or complex rendering.
    /// </summary>
    public class DwFormPrototype : Form
    {
        private Unity3DConnector u3d => Unity3DConnector.Instance;
        private int cameraId;
        private string formName;
        private Timer heartbeatTimer;
        private int heartbeatCount = 0;

        public DwFormPrototype(int cameraId)
        {
            this.cameraId = cameraId;
            this.formName = $"DwCamera_{cameraId}_Prototype";
            
            InitializeForm();
            StartHeartbeat();
            
            u3d.SendInfo($"DwFormPrototype for camera {cameraId} created.");
        }

        private void InitializeForm()
        {
            // Basic form settings
            this.Text = formName;
            this.TopMost = true;
            this.ShowInTaskbar = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 300);
            this.BackColor = Color.White;
            
            // Enable double buffering for smoother rendering
            this.DoubleBuffered = true;
            
            u3d.SendInfo($"DwFormPrototype for camera {cameraId} initialized.");
        }

        private void StartHeartbeat()
        {
            // Send heartbeat every 2 seconds to confirm the form is still alive
            heartbeatTimer = new Timer();
            heartbeatTimer.Interval = 2000;
            heartbeatTimer.Tick += OnHeartbeatTimer;
            heartbeatTimer.Start();
        }

        private void OnHeartbeatTimer(object sender, EventArgs e)
        {
            heartbeatCount++;
            u3d.SendInfo($"DwFormPrototype Camera {cameraId} heartbeat #{heartbeatCount} - Form is alive");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            
            // Draw background
            using (var bgBrush = new SolidBrush(Color.LightSteelBlue))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }
            
            // Draw title
            using (var titleFont = new Font("Arial", 20, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.DarkBlue))
            {
                var title = $"Prototype Camera {cameraId}";
                var titleSize = g.MeasureString(title, titleFont);
                var titleX = (Width - titleSize.Width) / 2;
                g.DrawString(title, titleFont, titleBrush, titleX, 50);
            }
            
            // Draw status info
            using (var infoFont = new Font("Arial", 12))
            using (var infoBrush = new SolidBrush(Color.Black))
            {
                var info = $"Form Name: {formName}\n" +
                          $"Size: {Width} x {Height}\n" +
                          $"Heartbeat Count: {heartbeatCount}\n" +
                          $"Created: {DateTime.Now:HH:mm:ss}";
                g.DrawString(info, infoFont, infoBrush, 20, 120);
            }
            
            // Draw border
            using (var borderPen = new Pen(Color.DarkBlue, 3))
            {
                g.DrawRectangle(borderPen, 2, 2, Width - 5, Height - 5);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            u3d.SendWarning($"DwFormPrototype Camera {cameraId} is closing. Reason: {e.CloseReason}");
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                heartbeatTimer?.Stop();
                heartbeatTimer?.Dispose();
                heartbeatTimer = null;
            }
            
            u3d.SendInfo($"DwFormPrototype for camera {cameraId} disposed.");
            base.Dispose(disposing);
        }
    }
}
