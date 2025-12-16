using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Share;

namespace WinOverlay
{
    public static class WoGpuWorkerExtensions
    {
        /// <summary>
        /// Reads bitmap data from shared memory and updates a WriteableBitmap for WPF.
        /// WriteableBitmap supports alpha blending in WPF using DirectX.
        /// </summary>
        /// <param name="worker">The GPU worker instance</param>
        /// <param name="info">Texture information</param>
        /// <param name="bitmap">The WriteableBitmap to update (will be created if null or size mismatch)</param>
        /// <returns>True if bitmap was successfully updated</returns>
        public static bool TryReadWriteableBitmap(this WoGpuWorker worker, TextureInfo info, ref WriteableBitmap bitmap)
        {
            try
            {
                // Create or recreate bitmap if needed
                if (bitmap == null || bitmap.PixelWidth != info.width || bitmap.PixelHeight != info.height)
                {
                    // Use Pbgra32 format which supports alpha blending in WPF
                    bitmap = new WriteableBitmap(
                        info.width, 
                        info.height, 
                        96, // dpiX
                        96, // dpiY
                        PixelFormats.Pbgra32, // Pre-multiplied BGRA with alpha
                        null); // palette
                }

                // Read pixel data from shared memory
                byte[] pixelData = worker.ReadPixelData(info);
                if (pixelData == null || pixelData.Length == 0)
                    return false;

                // Lock the bitmap for writing
                bitmap.Lock();

                try
                {
                    // Convert RGBA to BGRA format (Unity uses RGBA, WPF uses BGRA)
                    ConvertRGBAToBGRA(pixelData);

                    // Calculate stride (bytes per row)
                    int stride = info.width * 4; // 4 bytes per pixel (BGRA)

                    // Copy pixel data to WriteableBitmap
                    Int32Rect rect = new Int32Rect(0, 0, info.width, info.height);
                    bitmap.WritePixels(rect, pixelData, stride, 0);

                    // Mark the bitmap as dirty to trigger rendering
                    bitmap.AddDirtyRect(rect);
                }
                finally
                {
                    bitmap.Unlock();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading WriteableBitmap: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts RGBA format to BGRA format in-place.
        /// Unity uses RGBA, but WPF uses BGRA.
        /// </summary>
        private static void ConvertRGBAToBGRA(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                // Swap R and B channels
                byte temp = pixels[i];     // Store R
                pixels[i] = pixels[i + 2]; // R = B
                pixels[i + 2] = temp;      // B = R
                // G and A stay in the same position
            }
        }

        /// <summary>
        /// Reads raw pixel data from the GPU worker's shared memory.
        /// This method reads directly from the WOGpuWorker's pixel MMF accessor.
        /// </summary>
        private static byte[] ReadPixelData(this WoGpuWorker worker, TextureInfo info)
        {
            try
            {
                int expectedSize = info.width * info.height * 4; // RGBA = 4 bytes per pixel
                byte[] buffer = new byte[expectedSize];
                
                // Read from the pixels memory-mapped file using reflection
                // Get the private accessorPixels field from WOGpuWorker
                var accessorField = typeof(WoGpuWorker).GetField("m_AccessorPixels", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (accessorField != null)
                {
                    var accessor = accessorField.GetValue(worker) as System.IO.MemoryMappedFiles.MemoryMappedViewAccessor;
                    if (accessor != null && accessor.Capacity >= expectedSize)
                    {
                        accessor.ReadArray(0, buffer, 0, expectedSize);
                        return buffer;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading pixel data: {ex.Message}");
                return null;
            }
        }
    }
}
