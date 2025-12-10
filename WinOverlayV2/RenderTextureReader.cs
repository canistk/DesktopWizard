using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Share;

namespace WinOverlay
{
	/// <summary>
	/// Reads pixel data from a memory-mapped file and converts it to a WriteableBitmap for WPF.
	/// </summary>
	public class RenderTextureReader : IDisposable
	{
		private readonly string m_Name;
		private MemoryMappedFile mmfPixels;
		private MemoryMappedViewAccessor accessorPixels;
		private CancellationTokenSource cancel;
		private bool IsInitialized => mmfPixels != null && accessorPixels != null;

		public RenderTextureReader(string mmfPixelsName)
		{
			this.m_Name = mmfPixelsName;
			this.cancel = new CancellationTokenSource();
			Reinit();
		}

		private void Reinit()
		{
			Task.Run(() => WaitForInit(cancel.Token));
		}

		private async void WaitForInit(CancellationToken token)
		{
			mmfPixels?.Dispose();
			accessorPixels?.Dispose();

			while (mmfPixels == null && !token.IsCancellationRequested)
			{
				try
				{
					mmfPixels = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.Read);
				}
				catch (System.IO.FileNotFoundException)
				{
					continue;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[{m_Name}] Error opening pixel memory-mapped file. {ex.Message}");
					Reinit();
					return;
				}
				await Task.Delay(100, token);
			}

			if (mmfPixels != null)
			{
				// Create accessor with initial capacity
				const long initialCapacity = 1024 * 1024 * 10; // 10MB
				accessorPixels = mmfPixels.CreateViewAccessor(0, initialCapacity, MemoryMappedFileAccess.Read);
			}
		}

		public bool TryConvertToBitmap(in TextureInfo info, ref WriteableBitmap bitmap)
		{
			try
			{
				if (!IsInitialized)
					return false;

				int width = info.width;
				int height = info.height;
				int pixelCount = info.totalSize;

				if (pixelCount <= 0 || width <= 0 || height <= 0)
					return false;

				// Create or resize bitmap
				if (bitmap == null || bitmap.PixelWidth != width || bitmap.PixelHeight != height)
				{
					bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
				}

				// Read pixel data from memory-mapped file
				byte[] pixels = new byte[pixelCount];
				accessorPixels.ReadArray(0, pixels, 0, pixelCount);

				// Lock bitmap for direct memory access
				bitmap.Lock();

				try
				{
					// Copy pixels to bitmap (RGBA to BGRA conversion)
					const int bytesPerPixel = 4;
					int stride = bitmap.BackBufferStride;

					unsafe
					{
						byte* bmpPtr = (byte*)bitmap.BackBuffer;
						fixed (byte* srcPtr = pixels)
						{
							for (int y = 0; y < height; ++y)
							{
								// Unity's y-axis is flipped
								int srcY = height - 1 - y;
								byte* src = srcPtr + (srcY * width * bytesPerPixel);
								byte* dst = bmpPtr + (y * stride);

								for (int x = 0; x < width; ++x)
								{
									// Convert RGBA to BGRA
									dst[0] = src[2]; // B
									dst[1] = src[1]; // G
									dst[2] = src[0]; // R
									dst[3] = src[3]; // A

									src += bytesPerPixel;
									dst += bytesPerPixel;
								}
							}
						}
					}

					// Mark the entire bitmap as changed
					bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
				}
				finally
				{
					bitmap.Unlock();
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[{m_Name}] Error converting to bitmap: {ex.Message}");
				return false;
			}
		}

		public void Dispose()
		{
			cancel?.Cancel();
			cancel?.Dispose();
			cancel = null;

			accessorPixels?.Dispose();
			accessorPixels = null;

			mmfPixels?.Dispose();
			mmfPixels = null;
		}
	}
}
