using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
    public class DwForm : Form
	{
		private Unity3DConnector u3d => Unity3DConnector.Instance;
		private int cameraId;
        private string prefix;
        private GPUFunnel m_GPU01, m_GPU02;
		private Timer renderTimer;
        private Bitmap currentBitmap;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private bool isDisposed = false;

        [DllImport("d3d11.dll", SetLastError = true)]
        private static extern IntPtr D3D11CreateDevice(IntPtr pAdapter, int driverType, IntPtr software, uint flags, IntPtr pFeatureLevels, uint featureLevels, uint sdkVersion, out IntPtr ppDevice, IntPtr pFeatureLevel, IntPtr ppImmediateContext);

        [DllImport("d3d11.dll", SetLastError = true)]
        private static extern int ID3D11Device_OpenSharedResource(IntPtr device, IntPtr hResource, ref Guid riid, out IntPtr ppResource);

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
            
            InitializeSharedMemory();
            StartRenderTimer();
            u3d.SendInfo($"DwForm for camera {cameraId} created.");
		}

        private bool m_SharedMemoryInitialized = false;
		private void InitializeSharedMemory()
		{
            m_SharedMemoryInitialized = false;

			Task.Run(async () =>
            {
                int retryCount = 0;
                const int maxRetries = 30; // Wait up to 30 seconds
                u3d.SendWarning($"Attempting to connect to shared memory for camera {cameraId}...");
				while (retryCount < maxRetries && !isDisposed)
                {
                    try
                    {
                        m_GPU01 = new GPUFunnel($"{prefix}_1");
                        m_GPU02 = new GPUFunnel($"{prefix}_2");
                        m_SharedMemoryInitialized = true;
                        break;
                    }
                    catch (FileNotFoundException)
                    {
                        ++retryCount;
                        u3d.SendError($"Shared memory for camera {cameraId} not found. Retrying... ({retryCount}/{maxRetries})");
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

        private async void StartRenderTimer()
        {
            while (!m_SharedMemoryInitialized)
            {
                await Task.Delay(100);
            }
            renderTimer = new Timer();
            renderTimer.Interval = 16; // ~60 FPS
            renderTimer.Tick += OnRenderTimer;
            renderTimer.Start();
        }

        private bool m_Time2Update = false;
		private void OnRenderTimer(object sender, EventArgs e)
        {
            if (isDisposed)
                return;
            m_Time2Update = true;
			this.Invalidate();
		}

		protected override void OnPrint(PaintEventArgs e)
		{
			base.OnPrint(e);
            RenderDebugInfo(e);

            if (!m_Time2Update)
                return;
            UpdateFrame(e);
		}

        private void RenderDebugInfo(PaintEventArgs e)
        {
			var g = e.Graphics;

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
						  $"Created: {DateTime.Now:HH:mm:ss}";
				g.DrawString(info, infoFont, infoBrush, 20, 120);
			}
		}

        private void UpdateFrame(PaintEventArgs e)
        {
            // Read ShareInfo from both memory-mapped files
            m_GPU01.TryRead(out var shareInfo1);
            m_GPU02.TryRead(out var shareInfo2);

            // Select the oldest buffer based on timestamp
            ShareInfo latestShareInfo;
            if (shareInfo1.timestamp > shareInfo2.timestamp)
            {
                latestShareInfo = shareInfo1;
            }
            else
            {
                latestShareInfo = shareInfo2;
            }
            
            // Only update if we have new data
            if (latestShareInfo.timestamp <= lastUpdateTime || latestShareInfo.rtHandler == IntPtr.Zero)
                return;
                
            lastUpdateTime = latestShareInfo.timestamp;
            
            // Update form size if texture size changed
            if (Size.Width != latestShareInfo.width || Size.Height != latestShareInfo.height)
            {
                BeginInvoke(new Action(() =>
                {
                    Size = new Size(latestShareInfo.width, latestShareInfo.height);
                    // Keep the form centered
                    Location = new Point(
                        (Screen.PrimaryScreen.Bounds.Width - latestShareInfo.width) / 2,
                        (Screen.PrimaryScreen.Bounds.Height - latestShareInfo.height) / 2
                    );
                }));
            }

			// TODO: Implement DirectX texture reading using latestShareInfo.rtHandler
            ConvertDirectXTextureToBitmap(latestShareInfo, out Bitmap bitmap);
            if (bitmap != null)
            {
                BeginInvoke(new Action(() =>
                {
                    var oldBitmap = currentBitmap;
                    currentBitmap = bitmap;
                    oldBitmap?.Dispose();
                    Invalidate(); // Trigger repaint
                }));
			}
			// TODO: Implement OpenGL texture reading if needed
		}

		private void ConvertDirectXTextureToBitmap(ShareInfo data, out Bitmap bitmap)
        {
            throw new NotImplementedException();
        }


		protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // 添加測試繪製內容
            using (var brush = new SolidBrush(Color.LightBlue))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            
            using (var font = new Font("Arial", 16, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                var text = $"Camera {cameraId} Test Window";
                var textSize = e.Graphics.MeasureString(text, font);
                var x = (Width - textSize.Width) / 2;
                var y = (Height - textSize.Height) / 2;
                e.Graphics.DrawString(text, font, textBrush, x, y);
            }
            
            // 繪製邊框
            using (var pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, Width - 2, Height - 2);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
			base.OnFormClosing(e);
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    renderTimer?.Stop();
                    renderTimer?.Dispose();
                    currentBitmap?.Dispose();
                    m_GPU01?.Dispose();
                    m_GPU02?.Dispose();
                }
                isDisposed = true;
                m_SharedMemoryInitialized = false;

			}
            base.Dispose(disposing);
        }
    }
}