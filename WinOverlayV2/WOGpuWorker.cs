using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Share;

namespace WinOverlay
{
    /// <summary>
    /// Provides functionality to read GPU information from a memory-mapped file.
    /// </summary>
    /// <remarks>This class is designed to interact with a memory-mapped file that contains GPU-related data.
    /// It allows reading the shared GPU information in a thread-safe manner and ensures proper resource management by
    /// implementing <see cref="IDisposable"/>.</remarks>
	public class WoGpuWorker
    {
        private readonly string m_Name;
		private RenderTextureReader pixelReader;

		// extra information from GPU.
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
		private CancellationTokenSource cancel;
		private bool IsInitialized => mmf != null && accessor != null;
		public WoGpuWorker(string mmfName)
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

		public bool TryReadBitmap(in TextureInfo info, ref WriteableBitmap bitmap)
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

}