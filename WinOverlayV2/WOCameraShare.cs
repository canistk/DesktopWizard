using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using Share;

namespace WinOverlay
{
	/// <summary>
	/// Provides functionality to access and read camera matrix information from a memory-mapped file.
	/// </summary>
	/// <remarks>This class is designed to interact with a memory-mapped file that contains camera matrix
	/// data. It allows reading the data in a thread-safe manner and ensures proper resource management.</remarks>
	public class WoCameraShare : IDisposable
	{
		private MemoryMappedFile m_Mmf;
		private MemoryMappedViewAccessor m_Accessor;
		private CancellationTokenSource m_Cts;
		private readonly string m_Name;
		private const int MaxLength = 1024 * 1024; // 1 MB limit for safety
		private byte[] m_Buffer = new byte[MaxLength];

		private bool IsInitialized => m_Mmf != null && m_Accessor != null;
		public WoCameraShare(string mmfName)
		{
			this.m_Name = mmfName;
			this.m_Cts = new CancellationTokenSource();
			Task.Run(() => WaitForInit(m_Cts.Token));
		}
		private async void WaitForInit(CancellationToken token)
		{
			try
			{
				m_Mmf?.Dispose();
				m_Accessor?.Dispose();
				while (m_Mmf == null &&
					!token.IsCancellationRequested)
				{
					try
					{
						m_Mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.ReadWrite);
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

				if (m_Mmf != null && !token.IsCancellationRequested)
				{
					m_Accessor = m_Mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when disposing, ignore
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[{m_Name}] Unexpected error in WaitForInit: {ex.Message}");
			}
		}

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
				if (m_Accessor.Capacity < 4)
				{
					info = default;
					return false;
				}

				// Read the actual data
				m_Accessor.ReadArray(0, m_Buffer, 0, 4);
				if (!CameraInfo.IsValid(ref m_Buffer))
				{
					info = default;
					return false;
				}

				m_Accessor.ReadArray(4, m_Buffer, 4, CameraInfo.ByteArraySize);
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

		#region Dispose pattern
		public bool isDisposed { get; private set; } = false;
		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				isDisposed = true;
				if (disposing)
				{
					m_Cts?.Cancel();
					m_Cts?.Dispose();
					m_Accessor?.Dispose();
					m_Mmf?.Dispose();
				}
				m_Cts = null;
				m_Accessor = null;
				m_Mmf = null;
				m_Buffer = null;
			}
		}

		~WoCameraShare()
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
		#endregion Dispose pattern
	}

}
