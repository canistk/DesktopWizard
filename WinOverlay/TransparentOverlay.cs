using System;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
    public class TransparentOverlay : Form
    {
        private string cameraId;
        private MemoryMappedFile mmf0, mmf1, mmfFlag;
        private MemoryMappedViewAccessor accessor0, accessor1, flagAccessor;
        private NamedPipeClientStream infoPipe;
        private Timer renderTimer, heartbeatTimer;
        private Bitmap currentBitmap;
        private int currentBufferId = -1;
        private int currentTextureId = -1;
        private DateTime lastHeartbeat = DateTime.Now;

        public TransparentOverlay(OverlayConfig config)
        {
            this.cameraId = config.CameraId;
            
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            
            Bounds = config.Bounds;
            
            InitializeSharedMemory();
            InitializeInfoPipe();
            
            renderTimer = new Timer();
            renderTimer.Interval = 16; // ~60 FPS
            renderTimer.Tick += (s, e) => UpdateFrame();
            renderTimer.Start();
            
            heartbeatTimer = new Timer();
            heartbeatTimer.Interval = 5000; // 5 seconds
            heartbeatTimer.Tick += (s, e) => CheckHeartbeat();
            heartbeatTimer.Start();
        }

        private string GetPrefix()
        {
            return $"UC_{cameraId}";
		}

        private void InitializeSharedMemory()
        {
            Task.Run(async () =>
            {
                while (mmf0 == null || mmf1 == null || mmfFlag == null)
                {
                    try
                    {
                        var prefix = GetPrefix();
						mmf0 = MemoryMappedFile.OpenExisting($"{prefix}_0");
                        accessor0 = mmf0.CreateViewAccessor();
                        mmf1 = MemoryMappedFile.OpenExisting($"{prefix}_1");
                        accessor1 = mmf1.CreateViewAccessor();
                        mmfFlag = MemoryMappedFile.OpenExisting($"{prefix}_f");
                        flagAccessor = mmfFlag.CreateViewAccessor();
                        break;
                    }
                    catch
                    {
                        await Task.Delay(1000);
                    }
                }
            });
        }

        private void InitializeInfoPipe()
        {
            Task.Run(async () =>
            {
                var prefix = GetPrefix();
                while (true)
                {
                    try
                    {
                        infoPipe = new NamedPipeClientStream(".", $"{prefix}_info", PipeDirection.InOut);
                        await infoPipe.ConnectAsync();
                        StartInfoListener();
                        break;
                    }
                    catch
                    {
                        await Task.Delay(1000);
                    }
                }
            });
        }
        
        private void StartInfoListener()
        {
            Task.Run(async () =>
            {
                byte[] buffer = new byte[64];
                while (infoPipe?.IsConnected == true)
                {
                    try
                    {
                        int bytesRead = await infoPipe.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            lastHeartbeat = DateTime.Now;
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            });
        }
        
        private void CheckHeartbeat()
        {
            if ((DateTime.Now - lastHeartbeat).TotalSeconds > 10)
            {
                Application.Exit();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
        }

        private void UpdateFrame()
        {
			// TODO: on focus send mouse/keyboard events via NamedPipe to Unity3D
			// TODO: based on cameraId, read related ShareMemory
			// Read the flag to determine which buffer to use
		}

		protected override void OnPaint(PaintEventArgs e)
        {
            if (currentBitmap != null)
            {
                e.Graphics.DrawImage(currentBitmap, 0, 0, Width, Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                renderTimer?.Stop();
                renderTimer?.Dispose();
                heartbeatTimer?.Stop();
                heartbeatTimer?.Dispose();
                currentBitmap?.Dispose();
                accessor0?.Dispose();
                accessor1?.Dispose();
                flagAccessor?.Dispose();
                mmf0?.Dispose();
                mmf1?.Dispose();
                mmfFlag?.Dispose();
                infoPipe?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}