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
	#region GPU(s)
	
    /// <summary>
    /// Provides functionality to read GPU information from a memory-mapped file.
    /// </summary>
    /// <remarks>This class is designed to interact with a memory-mapped file that contains GPU-related data.
    /// It allows reading the shared GPU information in a thread-safe manner and ensures proper resource management by
    /// implementing <see cref="IDisposable"/>.</remarks>
	public class HSM_Gpu
    {
        private readonly string m_Name;
		private BitmapConverter pixelReader;

		// extra information from GPU.
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
		private CancellationTokenSource cancel;
		private bool IsInitialized => mmf != null && accessor != null;
		public HSM_Gpu(string mmfName)
        {
            this.m_Name = mmfName;
			this.pixelReader = new BitmapConverter($"{mmfName}_Pixels");
			this.cancel = new CancellationTokenSource();
			Reinit();
		}
		private void Reinit()
		{
			Task.Run(() => WaitForInit(cancel.Token));
		}

		private async void WaitForInit(CancellationToken token)
        {
            mmf?.Dispose();
			accessor?.Dispose();
			while (mmf == null &&
				!token.IsCancellationRequested)
			{
				try
				{
					mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.Read);
				}
				catch (System.IO.FileNotFoundException)
				{
					continue;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[{m_Name}] Error opening memory-mapped file. {ex.Message}");
					Reinit();
					return;
				}
				await Task.Delay(100, token);
			}

            accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<TextureInfo>(), MemoryMappedFileAccess.Read);
		}

		public DateTime GetTimestamp()
		{
			if (!IsInitialized)
				return DateTime.MinValue;
			return TextureInfo.FetchDatetime(accessor);
		}

		public bool TryRead(out TextureInfo info)
        {
            try
            {
				if (!IsInitialized)
					throw new Exception();
                info = new TextureInfo(accessor);
                return true;
            }
            catch
            {
                info = default;
                return false;
			}
		}

		public bool TryReadBitmap(in TextureInfo info, ref Bitmap bitmap)
		{
			return pixelReader.TryConvertToBitmap(info, ref bitmap);
		}


		public void Dispose()
		{
			pixelReader?.Dispose();
			pixelReader = null;
			accessor?.Dispose();
			accessor = null;
            mmf?.Dispose();
			mmf = null;
		}
	}

	// TODO: move ping-pong logic here
	public class HSM_PingPongGPU : System.IDisposable
	{
		private HSM_Gpu m_GPU01, m_GPU02;

		public HSM_PingPongGPU(string cameraPrefix)
		{
			m_GPU01 = new HSM_Gpu($"{cameraPrefix}_1");
			m_GPU02 = new HSM_Gpu($"{cameraPrefix}_2");
		}


		#region Dispose Pattern
		public bool IsDisposed { get; private set; }
		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					m_GPU01?.Dispose();
					m_GPU02?.Dispose();
				}

				m_GPU01 = null;
				m_GPU02 = null;
				IsDisposed = true;
			}
		}
		~HSM_PingPongGPU() => Dispose(disposing: false);
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion Dispose Pattern
	}
	#endregion GPU(s)

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
		private bool IsInitialized => mmf != null && accessor != null;
		public HSM_CameraMatrix(string mmfName, CancellationToken token)
		{
			this.m_Name = mmfName;
			Task.Run(() => WaitForInit(token));
		}
		private async void WaitForInit(CancellationToken token)
		{
			mmf?.Dispose();
			accessor?.Dispose();
			while (mmf == null &&
				!token.IsCancellationRequested)
			{
				try
				{
					mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.ReadWrite);
				}
				catch (System.IO.FileNotFoundException)
				{
					await Task.Delay(100, token);
					continue;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[{m_Name}] Error opening memory-mapped file. {ex.Message}");
					return;
				}
			}

			if (mmf != null)
			{
				accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
			}
		}
		
		const int MaxLength = 1024 * 1024; // 1 MB limit for safety
		byte[] m_Buffer = new byte[MaxLength];

		public bool TryRead(out CameraInfo info)
        {
            if (!IsInitialized)
            {
                info = default;
                return false;
            }
			try
            {
				// check if 4 bytes are available for length prefix
				if (accessor.Capacity < 4)
				{
					info = default;
					return false;
				}

				// Read the actual data
				accessor.ReadArray(0, m_Buffer, 0, 4);
				if (!CameraInfo.IsValid(ref m_Buffer))
				{
					info = default;
					return false;
				}

				accessor.ReadArray(4, m_Buffer, 4, CameraInfo.ByteArraySize);
            }
            catch
            {
                info = default;
                return false;
            }

			try
			{
				// ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(m_Buffer, 0, length);
				// Deserialize CameraInfo from bytes
				// info = CameraInfo.Parser.ParseFrom(span);
				info = CameraInfo.FromByteArray(ref m_Buffer);

				return true;
			}
			catch (Exception ex)
			{
				// Console print Hex dump of m_Buffer
				var hex = BitConverter.ToString(m_Buffer, 0, Math.Min(64, m_Buffer.Length)).Replace("-", " ");
				Console.WriteLine($"[{m_Name}] Error parsing CameraInfo: {ex.Message}\nHex Dump: {hex}");
				info = default;
				return false;
			}
		}
	}

	public class HSM_KeyboardMouse : System.IDisposable
	{
		public bool isDisposed { get; private set; }
        private NamedPipeServerStream m_Pipe = null;
        private CancellationToken m_CancelToken;
        public event Action OnClientConnected;
        public event Action OnClientDisconnected;

		public string name { get; private set; }
        public HSM_KeyboardMouse(string pipName)
        {
            this.m_State = eHSMState.Disconnected;
            this.name = pipName;
            this.m_Pipe = new NamedPipeServerStream(name, 
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
            this.m_Semaphore = new SemaphoreSlim(1);
		}

        public void Start(CancellationToken token)
        {
            if (state != eHSMState.Disconnected)
                return;
			this.m_CancelToken = token;
			Task.Run(WaitForConnection, m_CancelToken);
		}

		#region Handling Connection State
		
		private eHSMState m_State = eHSMState.Disconnected;
		public eHSMState state
		{
			get => m_State;
			private set
			{
				if (m_State == value)
					return;

				if (m_Pipe == null)
				{
					Console.WriteLine($"[{name}] Pipe is null, when trying to set state = {m_State}->{value}.");
					return;
				}
				m_State = value;
				switch (value)
				{
					case eHSMState.Disconnected:
					{
						if (m_Pipe.IsConnected)
						{
							m_Pipe.Disconnect();
							Console.WriteLine($"[{name}] Self Disconnected.");
						}
						OnClientDisconnected?.Invoke();
					}
					break;
					case eHSMState.WaitingForConnection:
					{
						Console.WriteLine($"[{name}] Waiting for connection...");
					}
					break;
					case eHSMState.Connected:
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
			try
			{
				state = eHSMState.WaitingForConnection;
				int attempts = 0;
				while (m_Pipe == null && attempts++ < 5)
					await Task.Delay(100);
				if (m_Pipe == null)
					throw new Exception("Pipe is null before WaitForConnectionAsync.");
				await m_Pipe.WaitForConnectionAsync(m_CancelToken);
				state = eHSMState.Connected;
				while (m_Pipe != null &&
					m_Pipe.IsConnected &&
					!m_CancelToken.IsCancellationRequested)
				{
					await Task.Delay(100);
				}
				state = eHSMState.Disconnected;
			}
			catch (Exception ex)
			{
				if (ex is OperationCanceledException)
				{
					Console.WriteLine($"[{name}] Connection attempt cancelled.");
				}
				else
				{
					Console.WriteLine($"[{name}] Error in WaitForConnection: {ex.Message}");
				}
			}
			finally
			{
				state = eHSMState.Disconnected;
				Dispose();
			}
		}
		#endregion Handling Connection State

		#region Handling Keyboard/Mouse Events

		private SemaphoreSlim m_Semaphore = new SemaphoreSlim(1);
		public void Send(KeyEventArgs e, bool isKeyUp)
		{
			var data = new KeyboardEventP3(e, isKeyUp).ToByteArray();
			Task.Run(() => SendByte(data), m_CancelToken);
		}

		public void Send(int state, MouseEventArgs e, CameraInfo cam, bool withinForm)
		{
			var data = new MouseEventP3(state, e, cam, withinForm).ToByteArray();
			Task.Run(() => SendByte(data), m_CancelToken);
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
				m_Pipe.Flush();
			}
			finally
			{
				m_Semaphore?.Release();
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
                    m_Pipe?.Dispose();
					m_Semaphore?.Dispose();
				}
                m_Pipe = null;
				m_Semaphore = null;
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

	public enum eHSMState
	{
		Disconnected = 0,
		WaitingForConnection,
		Connected,
	}
}