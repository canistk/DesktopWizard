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
        private MemoryMappedFile mmf1, mmf2;
        private MemoryMappedViewAccessor accessor1, accessor2;
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
            
            InitializeForm();
            InitializeSharedMemory();
            StartRenderTimer();
        }

        private void InitializeForm()
        {
            // Set up the overlay window
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            
            // Default size - will be updated based on texture size
            Size = new Size(640, 480);
        }

        private bool m_SharedMemoryInitialized = false;
		private void InitializeSharedMemory()
		{
            m_SharedMemoryInitialized = false;

			Task.Run(async () =>
            {
                int retryCount = 0;
                const int maxRetries = 30; // Wait up to 30 seconds
                
                while (retryCount < maxRetries && !isDisposed)
                {
                    try
                    {
						var sm1 = $"{prefix}_1";
                        var sm2 = $"{prefix}_2";
                        
                        mmf1 = MemoryMappedFile.OpenExisting(sm1);
                        accessor1 = mmf1.CreateViewAccessor();
                        
                        mmf2 = MemoryMappedFile.OpenExisting(sm2);
                        accessor2 = mmf2.CreateViewAccessor();
                        m_SharedMemoryInitialized = true;

						break;
                    }
                    catch (FileNotFoundException)
                    {
                        retryCount++;
                        u3d.SendError($"Shared memory for camera {cameraId} not found. Retrying... ({retryCount}/{maxRetries})");
						await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        u3d.SendError($"Error connecting to shared memory for camera {cameraId}: {ex.Message}");
						retryCount++;
                        await Task.Delay(1000);
                    }
                }
                
                if (retryCount >= maxRetries)
                {
                    u3d.SendError($"Failed to connect to shared memory for camera {cameraId} after multiple attempts.");
					// MessageBox.Show($"Failed to connect to shared memory for camera {cameraId}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BeginInvoke(new Action(() => Close()));
                }
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

        private void OnRenderTimer(object sender, EventArgs e)
        {
            if (isDisposed || accessor1 == null || accessor2 == null)
                return;

            try
            {
                UpdateFrame();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating frame: {ex.Message}");
            }
        }

        private void UpdateFrame()
        {
            // Read ShareInfo from both memory-mapped files
            var shareInfo1 = ReadShareInfo(accessor1);
            var shareInfo2 = ReadShareInfo(accessor2);
            
            // Select the most recent buffer based on timestamp
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
            
            /*
            // Convert the native texture handle to a bitmap and display it
            var newBitmap = CreateBitmapFromNativeTexture(latestShareInfo);
            if (newBitmap != null)
            {
                BeginInvoke(new Action(() =>
                {
                    var oldBitmap = currentBitmap;
                    currentBitmap = newBitmap;
                    oldBitmap?.Dispose();
                    Invalidate(); // Trigger repaint
                }));
            }
            //**/
        }

        private ShareInfo ReadShareInfo(MemoryMappedViewAccessor accessor)
        {
            if (accessor == null)
                return new ShareInfo();
                
            try
            {
                // Read the ShareInfo structure from shared memory
                var shareInfo = new ShareInfo();
                shareInfo.rtHandler = new IntPtr(accessor.ReadInt64(0));
                
                // Read DateTime - convert from binary
                var timestampBinary = accessor.ReadInt64(8);
                shareInfo.timestamp = DateTime.FromBinary(timestampBinary);
                
                shareInfo.width = accessor.ReadInt32(16);
                shareInfo.height = accessor.ReadInt32(20);
                shareInfo.rowPitch = accessor.ReadInt32(24);
                shareInfo.bytesPerPixel = accessor.ReadInt32(28);
                shareInfo.totalSize = accessor.ReadInt32(32);
                
                return shareInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading ShareInfo: {ex.Message}");
                return new ShareInfo();
            }
        }

        private Bitmap CreateBitmapFromNativeTexture(ShareInfo shareInfo)
        {
            // For now, create a placeholder bitmap with gradient
            // TODO: Implement actual DirectX texture reading using shareInfo.rtHandler
            
            if (shareInfo.width <= 0 || shareInfo.height <= 0)
                return null;
                
            try
            {
                var bitmap = new Bitmap(shareInfo.width, shareInfo.height, PixelFormat.Format32bppArgb);
                
                // Create a simple colored background as placeholder
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // Create a simple gradient effect manually
                    for (int x = 0; x < shareInfo.width; x++)
                    {
                        var ratio = (float)x / shareInfo.width;
                        var red = (int)(255 * ratio);
                        var blue = (int)(255 * (1 - ratio));
                        using (var brush = new SolidBrush(Color.FromArgb(255, red, 0, blue)))
                        using (var pen = new Pen(brush))
                        {
                            graphics.DrawLine(pen, x, 0, x, shareInfo.height);
                        }
                    }
                    
                    // Draw some info text
                    using (var font = new Font("Arial", 12))
                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        var text = $"Camera {cameraId}\n{shareInfo.width}x{shareInfo.height}\n{shareInfo.timestamp:HH:mm:ss.fff}";
                        graphics.DrawString(text, font, textBrush, 10, 10);
                    }
                }
                
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating bitmap: {ex.Message}");
                return null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (currentBitmap != null)
            {
                e.Graphics.DrawImage(currentBitmap, 0, 0, Width, Height);
            }
            else
            {
                // Draw placeholder when no image is available
                using (var brush = new SolidBrush(Color.FromArgb(128, Color.Gray)))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                
                using (var font = new Font("Arial", 16))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var text = $"Waiting for Camera {cameraId}...";
                    var textSize = e.Graphics.MeasureString(text, font);
                    var x = (Width - textSize.Width) / 2;
                    var y = (Height - textSize.Height) / 2;
                    e.Graphics.DrawString(text, font, textBrush, x, y);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isDisposed = true;
            m_SharedMemoryInitialized = false;

			base.OnFormClosing(e);
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
                    accessor1?.Dispose();
                    accessor2?.Dispose();
                    mmf1?.Dispose();
                    mmf2?.Dispose();
                }
                isDisposed = true;
                m_SharedMemoryInitialized = false;

			}
            base.Dispose(disposing);
        }
    }
}