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
	public class WoGpuWorker : IDisposable
    {
        private readonly string m_Name;
		private RenderTextureReader m_PixelReader;

		// extra information from GPU.
        private MemoryMappedFile m_Mmf;
        private MemoryMappedViewAccessor m_Accessor;
		private CancellationTokenSource m_Cts;
		
		private bool IsInitialized => m_Mmf != null && m_Accessor != null;
		public WoGpuWorker(string mmfName)
        {
            this.m_Name = mmfName;
			this.m_PixelReader = new RenderTextureReader($"{mmfName}_Pixels");
			this.m_Cts = new CancellationTokenSource();
			Reinit();
		}
		private void Reinit()
		{
			Task.Run(() => WaitForInit(m_Cts.Token));
		}

		private async void WaitForInit(CancellationToken token)
        {
            m_Mmf?.Dispose();
			m_Accessor?.Dispose();
			while (m_Mmf == null &&
				!token.IsCancellationRequested)
			{
				try
				{
					m_Mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.Read);
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

            m_Accessor = m_Mmf.CreateViewAccessor(0, Marshal.SizeOf<TextureInfo>(), MemoryMappedFileAccess.Read);
		}

		public DateTime GetTimestamp()
		{
			if (!IsInitialized)
				return DateTime.MinValue;
			return TextureInfo.FetchDatetime(m_Accessor);
		}

		public bool TryRead(out TextureInfo info)
        {
            try
            {
				if (!IsInitialized)
					throw new Exception();
                info = new TextureInfo(m_Accessor);
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
			return m_PixelReader.TryConvertToBitmap(info, ref bitmap);
		}

		#region Dispose Pattern
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
					m_PixelReader?.Dispose();
					m_Accessor?.Dispose();
					m_Mmf?.Dispose();
				}
				m_Cts = null;
				m_PixelReader = null;
				m_Accessor = null;
				m_Mmf = null;
			}
		}

		~WoGpuWorker()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion Dispose Pattern
	}

}