using Google.Protobuf;
using System;
using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
	/// <summary>
	/// Handle shared memory for GPU texture information.
	/// From KawaiOS to WinOverlay
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ShareGPUInfo
    {
        public IntPtr rtHandler;        // Native texture handle (platform-specific)
        public DateTime timestamp;      // UTC timestamp for synchronization
        public int width;               // Texture width in pixels
        public int height;              // Texture height in pixels  
        public int rowPitch;            // Row pitch in bytes (width * bytesPerPixel)
        public int bytesPerPixel;       // Bytes per pixel based on format
        public int totalSize;           // Total texture size in bytes

        public ShareGPUInfo(MemoryMappedViewAccessor accessor)
        {
            rtHandler       = (IntPtr)accessor.ReadInt64(0);
            timestamp       = DateTime.FromBinary(accessor.ReadInt64(8));
            width           = accessor.ReadInt32(16);
            height          = accessor.ReadInt32(20);
            rowPitch        = accessor.ReadInt32(24);
            bytesPerPixel   = accessor.ReadInt32(28);
            totalSize       = accessor.ReadInt32(32);
		}
	}
    /// <summary>
    /// Provides functionality to read GPU information from a memory-mapped file.
    /// </summary>
    /// <remarks>This class is designed to interact with a memory-mapped file that contains GPU-related data.
    /// It allows reading the shared GPU information in a thread-safe manner and ensures proper resource management by
    /// implementing <see cref="IDisposable"/>.</remarks>
	public class HSM_Gpu
    {
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
        private readonly string m_Name;
        public HSM_Gpu(string mmfName)
        {
            this.m_Name = mmfName;
            Reinit();
        }
        private void Reinit()
        {
            accessor?.Dispose();
            mmf?.Dispose();
			mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.Read);
            accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<ShareGPUInfo>(), MemoryMappedFileAccess.Read);
		}

		public bool TryRead(out ShareGPUInfo info)
        {
            try
            {
                if (accessor == null)
                {
                    Reinit();
                }
                info = new ShareGPUInfo(accessor);
                return true;
            }
            catch
            {
                info = default;
                return false;
			}
		}

        public void Dispose()
        {
            accessor?.Dispose();
            mmf?.Dispose();
		}
	}

	/// <summary>
	/// Handle shared memory for Camera MVP data.
	/// From KawaiOS to WinOverlay
	/// </summary>
	public struct CameraMatrixInfo
    {
        public WinOverlay.Mat4x4 o2m;    // OS to Monitor Matrix
        public WinOverlay.Mat4x4 m2f;    // Monitor to Form Matrix
        public WinOverlay.Vec2Int osPos; // Mouse pos in OS space
        public WinOverlay.Vec3 monPos;   // Transform mouse pos in Monitor space
        public WinOverlay.Vec3 formPos;  // Transform mouse pos in Form space

        public CameraMatrixInfo(MemoryMappedViewAccessor accessor)
        {
            // Read o2m: 16 floats at offset 0, column-major order
            float[] o2mFloats = new float[16];
            for (int i = 0; i < 16; i++) o2mFloats[i] = accessor.ReadSingle(i * 4);
            var o2mMatrix = new System.Numerics.Matrix4x4(
                o2mFloats[0], o2mFloats[1], o2mFloats[2], o2mFloats[3],
                o2mFloats[4], o2mFloats[5], o2mFloats[6], o2mFloats[7],
                o2mFloats[8], o2mFloats[9], o2mFloats[10], o2mFloats[11],
                o2mFloats[12], o2mFloats[13], o2mFloats[14], o2mFloats[15]
            );
            o2m = new Mat4x4(o2mMatrix);

            // Read m2f: 16 floats at offset 64, column-major order
            float[] m2fFloats = new float[16];
            for (int i = 0; i < 16; i++) m2fFloats[i] = accessor.ReadSingle(64 + i * 4);
            var m2fMatrix = new System.Numerics.Matrix4x4(
                m2fFloats[0], m2fFloats[1], m2fFloats[2], m2fFloats[3],
                m2fFloats[4], m2fFloats[5], m2fFloats[6], m2fFloats[7],
                m2fFloats[8], m2fFloats[9], m2fFloats[10], m2fFloats[11],
                m2fFloats[12], m2fFloats[13], m2fFloats[14], m2fFloats[15]
            );
            m2f = new Mat4x4(m2fMatrix);

            // Read osPos: two ints at offset 128
            int osX = accessor.ReadInt32(128);
            int osY = accessor.ReadInt32(132);
            osPos = new Vec2Int(osX, osY);

            // Read monPos: three floats at offset 136
            float monX = accessor.ReadSingle(136);
            float monY = accessor.ReadSingle(140);
            float monZ = accessor.ReadSingle(144);
            monPos = new Vec3(monX, monY, monZ);

            // Read formPos: three floats at offset 148
            float formX = accessor.ReadSingle(148);
            float formY = accessor.ReadSingle(152);
            float formZ = accessor.ReadSingle(156);
            formPos = new Vec3(formX, formY, formZ);
        }
    }
	
    /// <summary>
    /// Provides functionality to access and read camera matrix information from a memory-mapped file.
    /// </summary>
    /// <remarks>This class is designed to interact with a memory-mapped file that contains camera matrix
    /// data. It allows reading the data in a thread-safe manner and ensures proper resource management.</remarks>
	public class HSM_CameraMatrix
	{
		private MemoryMappedFile mmf;
		private MemoryMappedViewAccessor accessor;
		private readonly string m_Name;
		public HSM_CameraMatrix(string mmfName)
		{
			this.m_Name = mmfName;
			Reinit();
		}
		private void Reinit()
		{
			accessor?.Dispose();
			mmf?.Dispose();
			mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.Read);
			accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<CameraMatrixInfo>(), MemoryMappedFileAccess.Read);
		}
        public bool TryRead(out CameraMatrixInfo info)
        {
            try
            {
                if (accessor == null)
                {
                    Reinit();
                }
                info = new CameraMatrixInfo(accessor);
                return true;
            }
            catch
            {
                info = default;
                return false;
            }
		}
	}

	/// <summary>Protobuf class for keyboard event data.
    /// From WinOverlay to KawaiOS</summary>
	public partial class KeyboardEventP3
	{
		public KeyboardEventP3(KeyEventArgs e, bool isKeyUp)
		{
			KeyCode = (int)e.KeyCode;
			Alt = e.Alt;
			Control = e.Control;
			Shift = e.Shift;
			Handled = e.Handled;
			SuppressKeyPress = e.SuppressKeyPress;
			IsKeyUp = isKeyUp;
		}
	}

	/// <summary>Protobuf class for mouse event data.
    /// From WinOverlay to KawaiOS</summary>
	public partial class MouseEventP3
	{
		public MouseEventP3(MouseEventArgs e)
		{
			Button = (int)e.Button;
			Clicks = e.Clicks;
			X = e.X;
			Y = e.Y;
			Delta = e.Delta;
		}
	}

	public class HSM_KeyboardMouse : System.IDisposable
	{
		public bool isDisposed { get; private set; }
        private NamedPipeServerStream m_Pipe;
        private CancellationTokenSource m_CancelSrc;
        public event Action OnClientConnected;
        public event Action OnClientDisconnected;

		public string name { get; private set; }
        public HSM_KeyboardMouse(string pipName, CancellationTokenSource cancelSrc)
        {
            this.m_State = eState.Disconnected;
            this.name = pipName;
            this.m_Pipe = new NamedPipeServerStream(name, 
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
			this.m_CancelSrc = cancelSrc;
            this.m_Semaphore = new SemaphoreSlim(1);
		}

        public void Start()
        {
            if (state != eState.Disconnected)
                return;
			Task.Run(() => WaitForConnection());
		}

		#region Handling Connection State
		public enum eState
		{
			Disconnected = 0,
			WaitingForConnection,
			Connected,
		}
		private eState m_State = eState.Disconnected;
		public eState state
		{
			get => m_State;
			private set
			{
				if (m_Pipe == null)
					Console.WriteLine($"[{name}] Pipe is null, when trying to set state = {m_State}->{value}.");
				m_State = value;
				switch (value)
				{
					case eState.Disconnected:
					{
						if (m_Pipe.IsConnected)
						{
							m_Pipe.Disconnect();
							Console.WriteLine($"[{name}] Self Disconnected.");
						}
						OnClientDisconnected?.Invoke();
					}
					break;
					case eState.WaitingForConnection:
					{
						Console.WriteLine($"[{name}] Waiting for connection...");
					}
					break;
					case eState.Connected:
					{
						if (!m_Pipe.IsConnected)
						{
							Console.WriteLine($"[{name}] Pipe is not connected after connection attempt.");
						}
						Console.WriteLine($"[{name}] Connected.");
						OnClientConnected?.Invoke();
					}
					break;
				}
			}
		}

		private async void WaitForConnection()
        {
            while (!m_CancelSrc.IsCancellationRequested)
            {
                state = eState.WaitingForConnection;
			    await m_Pipe.WaitForConnectionAsync(m_CancelSrc.Token);
				state = eState.Connected;
                while (m_Pipe.IsConnected && !m_CancelSrc.IsCancellationRequested)
                {
                    await Task.Delay(100);
			    }
				state = eState.Disconnected;
            }
            Dispose();
		}
		#endregion Handling Connection State

		#region Handling Keyboard/Mouse Events
		private SemaphoreSlim m_Semaphore = new SemaphoreSlim(1);
		public void Send(KeyEventArgs e, bool isKeyUp)
		{
			var data = new KeyboardEventP3(e, isKeyUp).ToByteArray();
			Task.Run(() => SendByte(data));
		}

		public void Send(MouseEventArgs e)
		{
			var data = new MouseEventP3(e).ToByteArray();
			Task.Run(() => SendByte(data));
		}
		private async Task SendByte(byte[] data)
		{
			try
			{
				await m_Semaphore.WaitAsync();
				if (m_Pipe == null || !m_Pipe.IsConnected)
				{
					Console.WriteLine($"[{name}] Pipe is not connected when trying to send data.");
					return;
				}
				await m_Pipe.WriteAsync(data, 0, data.Length);
			}
			finally
			{
				m_Semaphore.Release();
			}
		}

		#endregion Handling Keyboard/Mouse Events

		#region Dispose Pattern
		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
                    m_Pipe?.Dispose();
					m_Semaphore?.Dispose();
				}
                m_Pipe = null;
				m_Semaphore = null;
				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				isDisposed = true;
			}
		}

        ~HSM_KeyboardMouse()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
		{
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}
		#endregion Dispose Pattern
	}
}