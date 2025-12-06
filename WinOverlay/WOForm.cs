using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
    public class WOForm : Form
	{
		private Unity3DConnector u3d => Unity3DConnector.Instance;
		private int cameraId;
        private string prefix;
		private const int MAX_GPU_WORKER = 4; // Note: same as U3D DwCamera.MAX_GPU_WORKER
		private HSM_Gpu[] m_GPU;
        private HSM_CameraMatrix m_CameraMatrix;
		private System.Windows.Forms.Timer renderTimer;
        private Bitmap currentBitmap;
        private bool isDisposed = false;
        
        // Bitmap converters for dual-buffer system
        private BitmapConverter m_Converter01, m_Converter02;
		private HSM_KeyboardMouse m_InputPipe;

		public WOForm(int cameraId)
        {
            this.cameraId = cameraId;
            this.prefix = $"DwCamera_{cameraId}";
            this.Text = this.prefix;
			// Set up the overlay window
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);

			this.TopMost = true;
			this.ShowInTaskbar = true; // for debug purpose
			this.BackColor = Color.Lime;
			this.TransparencyKey = Color.Lime;
			this.FormBorderStyle = FormBorderStyle.None;
			this.StartPosition = FormStartPosition.CenterScreen;

			// Default size - will be updated based on texture size
			this.Size = new Size(100, 200);
            
            // Initialize converters
            m_Converter01 = new BitmapConverter(prefix, 1);
            m_Converter02 = new BitmapConverter(prefix, 2);
            
            InitializeSharedMemory();
            InitializeInputPipe();
            InitializeCameraInfo();
			StartRenderTimer();
            u3d.SendInfo($"DwForm for camera {cameraId} created.");
		}

        private bool m_SharedMemoryInitialized = false;
		private void InitializeSharedMemory()
		{
            m_SharedMemoryInitialized = false;
			// var shareName = $"{MMF_NAME}_{m_CameraId}_Matrix";
            
			Task.Run(async () =>
            {
                int retryCount = 0;
                const int maxRetries = 30; // Wait up to 30 seconds
                u3d.SendWarning($"Attempting to connect to shared memory for camera {cameraId}...");
				while (retryCount < maxRetries && !isDisposed)
                {
                    try
                    {
                        m_GPU = new HSM_Gpu[MAX_GPU_WORKER];
						for (int i = 0; i < MAX_GPU_WORKER; ++i)
                        {
                            m_GPU[i] = new HSM_Gpu($"{prefix}_{i}");
                        }
                        m_SharedMemoryInitialized = true;
                        break;
                    }
                    catch (FileNotFoundException ex)
                    {
                        ++retryCount;
                        u3d.SendError($"Shared memory for camera {cameraId} not found. Retrying... ({retryCount}/{maxRetries})\n{ex.Message}");
						await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        u3d.SendError($"Error connecting to shared memory for camera {cameraId}: {ex.Message}");
						++retryCount;
                        await Task.Delay(1000);
                    }
                }
				if (retryCount >= maxRetries)
                {
                    u3d.SendError($"Failed to connect to shared memory for camera {cameraId} after multiple attempts.");
					// MessageBox.Show($"Failed to connect to shared memory for camera {cameraId}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BeginInvoke(new Action(() => Close()));
                }
				u3d.SendWarning($"Connected to shared memory for camera {cameraId}.");
            });
        }

        private void InitializeCameraInfo()
        {
            m_CameraMatrix = new HSM_CameraMatrix($"{prefix}_Info", m_CancelSrc.Token);
            Console.WriteLine($"Camera info for camera {cameraId} initialized.");
		}

        private async void StartRenderTimer()
        {
            while (!m_SharedMemoryInitialized)
            {
                await Task.Delay(100);
            }
            renderTimer = new System.Windows.Forms.Timer();
#if true
			renderTimer.Tick += OnRenderTimer;
            SetTargetFPS(DEFAULT_FPS);
#else
            renderTimer.Interval = 1; // ASAP
			renderTimer.Tick += ASAPRender;
            renderTimer.Start();
#endif
        }

        private const int DEFAULT_FPS = 30;
		private int m_TargetFPS = 1;
		private void SetTargetFPS(int fps)
        {
            if (m_TargetFPS == fps)
                return;
			if (renderTimer != null && fps > 0)
            {
                var val = 1000.0f / fps;
                if (val < 0f) val = 1f; else if (val > 1000) val = 1000;
                renderTimer.Stop();
				renderTimer.Interval = Convert.ToInt32(val);
                renderTimer.Start();
            }
		}

        private void ASAPRender(object sender, EventArgs e)
		{
            if (isDisposed || !m_SharedMemoryInitialized)
                return;
            for (int i = 0; i < MAX_GPU_WORKER; ++i)
            {
				// Any new frame available?
				var t = m_GPU[i].GetTimestamp();
                if (m_LastRenderTime < t)
                {
                    OnRenderTimer(this, EventArgs.Empty);
                    break;
				}
			}
		}

		private DateTime m_LastRenderTime = DateTime.MinValue;
		private void OnRenderTimer(object sender, EventArgs e)
        {
            if (isDisposed || !m_SharedMemoryInitialized)
                return;

			KeyValuePair<int, TextureInfo> anchor = new KeyValuePair<int, TextureInfo>(-1, default);
            for (int i = 0; i < MAX_GPU_WORKER; ++i)
            {
                if (!m_GPU[i].TryRead(out var info))
                    continue;
                if (info.timestamp > m_LastRenderTime)
                {
					// Found a new frame
					// Search for the oldest frame non-display frame
					anchor = new KeyValuePair<int, TextureInfo>(i, info);
				}
			}
            if (anchor.Key == -1)
                return; // No new frame

            m_LastRenderTime = anchor.Value.timestamp;
			ReadBitmap(m_GPU[anchor.Key], anchor.Value);
			return;

            void ReadBitmap(HSM_Gpu gpu, TextureInfo data)
            {
                if (gpu.TryReadBitmap(data, ref currentBitmap))
                {
                    Invalidate(); // Trigger repaint
				}

				// Set location and size of this form.
				SetFormRect(data);
            }
            void SetFormRect(TextureInfo data)
			{
                Point location = new Point(
                    (Screen.PrimaryScreen.Bounds.Width - data.width) / 2,
					(Screen.PrimaryScreen.Bounds.Height - data.height) / 2
				);

				if (m_CameraMatrix != null && m_CameraMatrix.TryRead(out var camInfo))
                {
					// Test: offset by form OS position
					location = new Point(camInfo.FormOSPos.X + 100, camInfo.FormOSPos.Y + 100);
                }
				BeginInvoke(new Action(() =>
				{
					Size = new Size(data.width, data.height);
                    Location = location;
				}));
			}
		}

        private void RenderDebugInfo(Graphics g)
        {
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
				var info =
						  $"Size: {Width} x {Height}\n" +
						  $"Last Update: {m_LastRenderTime:HH:mm:ss.fff}";
				g.DrawString(info, infoFont, infoBrush, 20, 120);
			}
		}

        // Remove old UpdateFrame method - moved to OnRenderTimer
        static readonly Point s_LeftTop = new Point(0, 0);
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw current bitmap if available
            if (currentBitmap != null)
            {
                e.Graphics.DrawImage(currentBitmap, s_LeftTop);
            }
            else
            {
                using (var font = new Font("Arial", 16, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.Black))
                {
                    var text = $"Camera {cameraId} (Waiting for texture...)";
                    var textSize = e.Graphics.MeasureString(text, font);
                    var x = (Width - textSize.Width) / 2;
                    var y = (Height - textSize.Height) / 2;
                    e.Graphics.DrawString(text, font, textBrush, x, y);
                }
            }

            using (var pen = new Pen(Color.Red, 4))
            {
                // convert seconds to angle: 360 degrees in 60 seconds
                var sec = m_LastRenderTime.Second;
                var angle = (sec / 60.0f) * 360.0f;
				e.Graphics.DrawArc(pen, 10, 10, 30, 30, 0, (int)angle);
            }

			// Show debug info while waiting
			RenderDebugInfo(e.Graphics);
            
            // Draw border
            // using (var pen = new Pen(Color.Red, 2)) { e.Graphics.DrawRectangle(pen, 1, 1, Width - 2, Height - 2); }
        }

		protected override void OnFormClosing(FormClosingEventArgs e)
        {
			base.OnFormClosing(e);
            Dispose(true);
        }

		#region Mouse Events
		private CancellationTokenSource m_CancelSrc = null;

        private void InitializeInputPipe()
		{
            if (m_CancelSrc == null)
            {
                m_CancelSrc = new CancellationTokenSource();
            }
            else
            {
                m_CancelSrc.Cancel();
                m_CancelSrc.Dispose();
                m_CancelSrc = new CancellationTokenSource();
			}
            m_InputPipe = new HSM_KeyboardMouse($"{prefix}_InputPipe");
            m_InputPipe.Start(m_CancelSrc.Token);
		}

		/// <summary>Check mouse event within form bounds</summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private bool IsMouseWithinForm(MouseEventArgs e)
        {
            var formPos = this.PointToScreen(e.Location);
            var formRect = new Rectangle(this.Location, this.Size);
            return formRect.Contains(formPos);
		}

		protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

			if (!m_CameraMatrix.TryRead(out var camInfo))
				return;
			m_InputPipe.Send(0, e, camInfo, IsMouseWithinForm(e));
		}

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
			if (!m_CameraMatrix.TryRead(out var camInfo))
				return;
			m_InputPipe.Send(1, e, camInfo, IsMouseWithinForm(e));
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (!m_CameraMatrix.TryRead(out var camInfo))
				return;
			m_InputPipe.Send(2, e, camInfo, IsMouseWithinForm(e));
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
            base.OnMouseWheel(e);
			
            if (!m_CameraMatrix.TryRead(out var camInfo))
                return;
            m_InputPipe.Send(3, e, camInfo, IsMouseWithinForm(e));
		}
		#endregion Mouse Events

		#region Keyboard Events
		protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
			m_InputPipe.Send(e, false);

		}
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
			m_InputPipe.Send(e, true);
		}
		#endregion Keyboard Events

		protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
			{
				isDisposed = true;
				if (disposing)
                {
                    renderTimer?.Stop();
                    renderTimer?.Dispose();
                    currentBitmap?.Dispose();
                    for (int i = 0; i < MAX_GPU_WORKER; ++i)
                    {
                        m_GPU[i]?.Dispose();
                        m_GPU[i] = null;
					}
                    m_Converter01?.Dispose();
                    m_Converter02?.Dispose();
                    m_InputPipe?.Dispose();
                    m_CancelSrc?.Dispose();
				}
                renderTimer = null;
				currentBitmap = null;
				m_GPU = null;
				m_Converter01 = null;
				m_Converter02 = null;
				m_InputPipe = null;
				m_CancelSrc = null;
                m_SharedMemoryInitialized = false;

			}
            base.Dispose(disposing);
        }
    }
}