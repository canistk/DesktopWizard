using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Share;

namespace WinOverlay
{
    public class WoWindow : Window, IDisposable
    {
        private WoMessagePipe u3d => WoMessagePipe.Instance;
        private int cameraId;
        private string prefix;
        private const int MAX_GPU_WORKER = 4; // Note: same as U3D DwCamera.MAX_GPU_WORKER
        private WoGpuWorker[] m_GPU;
        private WoCameraShare m_CameraShare;
        public WoCameraShare CameraShare => m_CameraShare;

        private System.Windows.Threading.DispatcherTimer renderTimer;
        private WriteableBitmap currentBitmap;
        
        private WoWindowInputPipe m_InputPipe;
        public WoWindowInputPipe InputPipe => m_InputPipe;

		private CancellationTokenSource m_Cts;

        public WoWindow(int cameraId)
        {
            this.cameraId = cameraId;
            this.prefix = $"DwCamera_{cameraId}";
            this.Title = this.prefix;

            // Set up the overlay window
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Background = Brushes.Transparent;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.ShowInTaskbar = true; // for debug purpose
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Default size - will be updated based on texture size
            this.Width = 100;
            this.Height = 200;

            // Create Image control for rendering
            var image = new System.Windows.Controls.Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            this.Content = image;
            this.m_Cts = new CancellationTokenSource();

			InitializeGpuWorkers();
            InitializeInputPipe();
            InitializeCameraShare();
            StartRenderTimer();
            
            u3d.SendInfo($"WoWpf for camera {cameraId} created.");
        }

        private bool m_GpuWorkersInitialized = false;
        private void InitializeGpuWorkers()
        {
            m_GpuWorkersInitialized = false;
            Task.Run(ConnectGpuWorkers, m_Cts.Token);
        }
        private async Task ConnectGpuWorkers()
        {
			int retryCount = 0;
			const int maxRetries = 30; // Wait up to 30 seconds
			u3d.SendWarning($"Attempting to connect to shared memory for camera {cameraId}...");
            var token = m_Cts.Token;
			while (retryCount < maxRetries && !isDisposed)
			{
				try
				{
					m_GPU = new WoGpuWorker[MAX_GPU_WORKER];
					for (int i = 0; i < MAX_GPU_WORKER; ++i)
					{
						m_GPU[i] = new WoGpuWorker($"{prefix}_{i}");
					}
					m_GpuWorkersInitialized = true;
					break;
				}
				catch (System.IO.FileNotFoundException ex)
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
            if (isDisposed || token.IsCancellationRequested)
                return;
			if (retryCount >= maxRetries)
			{
				u3d.SendError($"Failed to connect to shared memory for camera {cameraId} after multiple attempts.");
				Dispatcher.Invoke(() => Close());
			}

			u3d.SendWarning($"Connected to shared memory for camera {cameraId}.");
		}

        private void InitializeCameraShare()
        {
            m_CameraShare = new WoCameraShare($"{prefix}_Info");
            Console.WriteLine($"Camera info for camera {cameraId} initialized.");
        }

        private async void StartRenderTimer()
        {
            while (!m_GpuWorkersInitialized)
            {
                await Task.Delay(100);
            }
            
            renderTimer = new System.Windows.Threading.DispatcherTimer();
            renderTimer.Tick += OnRenderTimer;
            SetTargetFPS(DEFAULT_FPS);
        }

        private const int DEFAULT_FPS = 30;
        private int m_TargetFPS = 1;
        private void SetTargetFPS(int fps)
        {
            if (m_TargetFPS == fps)
                return;
            
            if (renderTimer != null && fps > 0)
            {
                var val = 1000.0 / fps;
                if (val < 1.0) val = 1.0;
                else if (val > 1000) val = 1000;
                renderTimer.Stop();
                renderTimer.Interval = TimeSpan.FromMilliseconds(val);
                renderTimer.Start();
            }
        }

        private DateTime m_LastRenderTime = DateTime.MinValue;
		
		private void OnRenderTimer(object sender, EventArgs e)
        {
            if (isDisposed || !m_GpuWorkersInitialized)
                return;

            System.Collections.Generic.KeyValuePair<int, TextureInfo> anchor = 
                new System.Collections.Generic.KeyValuePair<int, TextureInfo>(-1, default);
            
            for (int i = 0; i < MAX_GPU_WORKER; ++i)
            {
                if (!m_GPU[i].TryRead(out var info))
                    continue;
                
                if (info.timestamp > m_LastRenderTime)
                {
                    // Found a new frame
                    anchor = new System.Collections.Generic.KeyValuePair<int, TextureInfo>(i, info);
                }
            }
            
            if (anchor.Key == -1)
                return; // No new frame

            m_LastRenderTime = anchor.Value.timestamp;
            ReadBitmap(m_GPU[anchor.Key], anchor.Value);
        }

        private void ReadBitmap(WoGpuWorker gpu, TextureInfo data)
        {
            if (gpu.TryReadBitmap(data, ref currentBitmap))
            {
                // Update the Image control
                if (Content is System.Windows.Controls.Image image)
                {
                    image.Source = currentBitmap;
                }
            }

            // Set location and size of this window
            SetWindowRect(data);
        }

        private void SetWindowRect(TextureInfo data)
        {
            double left = (SystemParameters.PrimaryScreenWidth - data.width) / 2;
            double top = (SystemParameters.PrimaryScreenHeight - data.height) / 2;

            if (m_CameraShare != null && m_CameraShare.TryRead(out var camInfo))
            {
                // Test: offset by form OS position
                left = camInfo.FormOSPos.X + 100;
                top = camInfo.FormOSPos.Y + 100;
            }

            Dispatcher.Invoke(() =>
            {
                Width = data.width;
                Height = data.height;
                Left = left;
                Top = top;
            });
        }

        private void InitializeInputPipe()
        {
            m_InputPipe = new WoWindowInputPipe($"{prefix}_InputPipe", this);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose(true);
        }

        public bool IsContain(Point point)
        {
            return
                point.X >= Left &&
                point.X <= Left + Width &&
                point.Y >= Top &&
                point.Y <= Top + Height;
		}


        private bool IsClosing()
        {
            return !IsVisible || PresentationSource.FromVisual(this) == null;
        }

		#region Dispose

		private bool isDisposed = false;
		protected void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (disposing)
				{
                    renderTimer?.Stop();
                    m_InputPipe?.Dispose();

					for (int i = 0; i < MAX_GPU_WORKER; ++i)
                    {
                        m_GPU[i]?.Dispose();
                        m_GPU[i] = null;
                    }

                    m_Cts?.Dispose();
                    m_CameraShare?.Dispose();

                    try
                    {
                        if (Dispatcher.CheckAccess())
                        {
                            if (!IsClosing())
                                Close();
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (!IsClosing())
                                    Close();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        u3d.SendError($"Error disposing WoWindow for camera {cameraId}: {ex.Message}");
					}
				}

				renderTimer = null;
				currentBitmap = null;
                m_Cts = null;
                m_InputPipe = null;
                m_CameraShare = null;
				m_GPU = null;
                m_GpuWorkersInitialized = false;
            }
        }

        ~WoWindow()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion Dispose
	}
}
