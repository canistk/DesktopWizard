using Google.Protobuf;
using System;
using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Share;

namespace WinOverlay
{
	#region GPU(s)
	
    /// <summary>
    /// Provides functionality to read GPU information from a memory-mapped file.
    /// </summary>
    /// <remarks>This class is designed to interact with a memory-mapped file that contains GPU-related data.
    /// It allows reading the shared GPU information in a thread-safe manner and ensures proper resource management by
    /// implementing <see cref="IDisposable"/>.</remarks>
	public class WOGpuWorker
    {
        private readonly string m_Name;
		private RenderTextureReader pixelReader;

		// extra information from GPU.
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
		private CancellationTokenSource cancel;
		private bool IsInitialized => mmf != null && accessor != null;
		public WOGpuWorker(string mmfName)
        {
            this.m_Name = mmfName;
			this.pixelReader = new RenderTextureReader($"{mmfName}_Pixels");
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
	#endregion GPU(s)

    /// <summary>
    /// Provides functionality to access and read camera matrix information from a memory-mapped file.
    /// </summary>
    /// <remarks>This class is designed to interact with a memory-mapped file that contains camera matrix
    /// data. It allows reading the data in a thread-safe manner and ensures proper resource management.</remarks>
	public class WOCameraShare
	{
		private MemoryMappedFile mmf;
		private MemoryMappedViewAccessor accessor;
		private readonly string m_Name;
		private bool IsInitialized => mmf != null && accessor != null;
		public WOCameraShare(string mmfName, CancellationToken token)
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

}