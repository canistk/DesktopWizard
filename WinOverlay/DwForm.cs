using Google.Protobuf;
using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
    public class DwForm : Form
	{
		private Unity3DConnector u3d => Unity3DConnector.Instance;
		private int cameraId;
        private string prefix;
        private HSM_Gpu m_GPU01, m_GPU02;
        private HSM_CameraMatrix m_CameraMatrix;
		private System.Windows.Forms.Timer renderTimer;
        private Bitmap currentBitmap;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private bool isDisposed = false;
        
        // Bitmap converters for dual-buffer system
        private BitmapConverter m_Converter01, m_Converter02;
        //private NamedPipeServerStream m_InputPipe;
		private HSM_KeyboardMouse m_InputPipe;

		public DwForm(int cameraId)
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
                        m_GPU01 = new HSM_Gpu($"{prefix}_1");
                        m_GPU02 = new HSM_Gpu($"{prefix}_2");
                        //m_CameraMatrix = new HSM_CameraMatrix($"{prefix}_Matrix");
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

        private async void InitializeCameraInfo()
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
            //renderTimer.Interval = m_TargetFPS; // ~60 FPS
            //renderTimer.Tick += OnRenderTimer;
            
            renderTimer.Interval = 1; // ASAP
			renderTimer.Tick += ASAPRender;
			renderTimer.Start();
        }

        private int m_TargetFPS = 60;
		private void SetTargetFPS(int fps)
        {
            if (m_TargetFPS == fps)
                return;
			if (renderTimer != null && fps > 0)
            {
                var val = 1000.0f / fps;
                if (val < 0f) val = 0f; else if (val > 1000) val = 1000;
                renderTimer.Interval = Convert.ToInt32(val);
            }
		}

        private void ASAPRender(object sender, EventArgs e)
		{
            if (isDisposed || !m_SharedMemoryInitialized)
                return;
            var t1 = m_GPU01.GetTimestamp();
            var t2 = m_GPU02.GetTimestamp();
			if (m_LastRenderTime < t1 || m_LastRenderTime < t2)
            {
                OnRenderTimer(this, EventArgs.Empty);
			}
		}

        private bool m_Rendering = false;
		private DateTime m_LastRenderTime = default;
		private void OnRenderTimer(object sender, EventArgs e)
        {
            if (isDisposed || !m_SharedMemoryInitialized)
                return;
            if (m_Rendering)
                return;
			m_Rendering = true;
			// Read ShareInfo from both memory-mapped files
			m_GPU01.TryRead(out var shareInfo1);
            m_GPU02.TryRead(out var shareInfo2);

			// Found oldest non-display frame.
			var g1 = shareInfo1.timestamp - m_LastRenderTime;
            var g2 = shareInfo2.timestamp - m_LastRenderTime;
            if (g1.Duration() < g2.Duration())
            {
                m_LastRenderTime = shareInfo1.timestamp;
				ReadBitmap(m_GPU01, shareInfo1);
			}
            else
            {
                m_LastRenderTime = shareInfo2.timestamp;
                ReadBitmap(m_GPU02, shareInfo2);
			}
            m_Rendering = false;
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
					location = new Point(camInfo.FormOSPosX + 300, camInfo.FormOSPosY);
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
						  $"Last Update: {lastUpdateTime:HH:mm:ss.fff}";
				g.DrawString(info, infoFont, infoBrush, 20, 120);
			}
		}

        // Remove old UpdateFrame method - moved to OnRenderTimer

		protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw current bitmap if available
            if (currentBitmap != null)
            {
                e.Graphics.DrawImage(currentBitmap, 0, 0, Width, Height);
            }
            else
            {
                // Fallback: draw debug info when no bitmap is available
                using (var brush = new SolidBrush(Color.LightBlue))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                
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


		protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            m_InputPipe.Send(e);
		}

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
			m_InputPipe.Send(e);
		}

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
			m_InputPipe.Send(e);
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
                    m_GPU01?.Dispose();
                    m_GPU02?.Dispose();
                    m_Converter01?.Dispose();
                    m_Converter02?.Dispose();
                    m_InputPipe?.Dispose();
                    m_CancelSrc?.Dispose();
				}
                renderTimer = null;
				currentBitmap = null;
				m_GPU01 = null;
                m_GPU02 = null;
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