using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.MemoryMappedFiles;

namespace WinOverlay
{
    /// <summary>
    /// Converts pixel data from Unity's shared memory to System.Drawing.Bitmap.
    /// Handles RGBA to BGRA format conversion and y-axis flipping.
    /// </summary>
    public class BitmapConverter : IDisposable
    {
        private MemoryMappedFile mmfPixels;
        private MemoryMappedViewAccessor accessorPixels;
        private bool isDisposed = false;
        private readonly string pixelsMmfName;
        
        public BitmapConverter(string cameraPrefix, int subId)
        {
            pixelsMmfName = $"{cameraPrefix}_{subId}_Pixels";
		}

        public BitmapConverter(string pixelsMmfName)
        {
            this.pixelsMmfName = pixelsMmfName;
        }


		byte[] m_Pixels = null;

		/// <summary>
		/// Converts shared memory pixel data to a Bitmap.
		/// Handles Unity RGBA ? Windows BGRA conversion and y-axis flipping.
		/// </summary>
		public bool TryConvertToBitmap(TextureInfo shareInfo, ref Bitmap bitmap)
        {
            // bitmap = null;
            
            if (shareInfo.totalSize <= 0)
                return false;

			try
			{
                var size = shareInfo.totalSize;
                if (mmfPixels == null)
                {
				    mmfPixels = MemoryMappedFile.OpenExisting(pixelsMmfName, MemoryMappedFileRights.Read);
                }

                if (accessorPixels == null || accessorPixels.Capacity < size)
                {
                    accessorPixels?.Dispose();
                    accessorPixels = null;
                }
				accessorPixels = mmfPixels.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[BitmapConverter] Failed to open pixel MMF '{pixelsMmfName}': {ex.Message}");
			}

			try
            {
                // Verify MMF has enough capacity
                if (accessorPixels.Capacity < shareInfo.totalSize)
                {
                    Console.WriteLine($"[BitmapConverter] MMF capacity ({accessorPixels.Capacity}) < required size ({shareInfo.totalSize})");
                    return false;
                }
                
                // Read pixel data from MMF
                if (m_Pixels == null || m_Pixels.Length < shareInfo.totalSize)
                {
                    m_Pixels = new byte[shareInfo.totalSize];
				}
                accessorPixels.ReadArray(0, m_Pixels, 0, shareInfo.totalSize);
				
                // Create bitmap
                if (bitmap == null ||
                    bitmap.Width != shareInfo.width ||
                    bitmap.Height != shareInfo.height ||
                    bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    bitmap = new Bitmap(shareInfo.width, shareInfo.height, PixelFormat.Format32bppArgb);
                }
                
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);
                
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride;
                    int width = shareInfo.width;
                    int height = shareInfo.height;
                    
                    for (int y = 0; y < height; y++)
                    {
                        // Unity's y-axis is flipped: upward = y++, downward = y--
                        // Windows y-axis: upward = y--, downward = y++
                        int srcY = height - 1 - y;
                        
                        for (int x = 0; x < width; x++)
                        {
                            // Unity RGBA format: [R][G][B][A]
                            int srcIdx = (srcY * width + x) * 4;
                            
                            // Windows BGRA format: [B][G][R][A]
                            int dstIdx = y * stride + x * 4;
                            
                            // Convert RGBA ? BGRA
                            ptr[dstIdx + 0] = m_Pixels[srcIdx + 2]; // B ? B (Unity stores as RGBA but actually BGRA in memory)
                            ptr[dstIdx + 1] = m_Pixels[srcIdx + 1]; // G ? G
                            ptr[dstIdx + 2] = m_Pixels[srcIdx + 0]; // R ? R
                            ptr[dstIdx + 3] = m_Pixels[srcIdx + 3]; // A ? A
                        }
                    }
                }
                
                bitmap.UnlockBits(bmpData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BitmapConverter] Error converting to bitmap: {ex.Message}");
                bitmap?.Dispose();
                bitmap = null;
                return false;
            }
        }
        
        public void Dispose()
        {
            if (!isDisposed)
            {
                accessorPixels?.Dispose();
                mmfPixels?.Dispose();
                isDisposed = true;
            }
            m_Pixels = null;
		}
    }
}
