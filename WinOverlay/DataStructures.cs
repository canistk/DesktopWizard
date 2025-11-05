using System;
using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace WinOverlay
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InputEvent
    {
        public byte Type;        // 0=MouseMove, 1=MouseDown, 2=MouseUp, 3=KeyDown, 4=KeyUp
        public byte CameraId;    
        public short X, Y;       
        public int Data;         // MouseButton or KeyCode
        public long Timestamp;   
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameInfo
    {
        public int FrameId;
        public float Fps;
        public byte WriteFlag;   // 0=Idle, 1=Writing
        public long Timestamp;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ShareInfo
    {
        public IntPtr rtHandler;        // Native texture handle (platform-specific)
        public DateTime timestamp;      // UTC timestamp for synchronization
        public int width;               // Texture width in pixels
        public int height;              // Texture height in pixels  
        public int rowPitch;            // Row pitch in bytes (width * bytesPerPixel)
        public int bytesPerPixel;       // Bytes per pixel based on format
        public int totalSize;           // Total texture size in bytes

        public ShareInfo(MemoryMappedViewAccessor accessor)
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

    public class GPUFunnel
    {
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
        private readonly string m_Name;
        public GPUFunnel(string mmfName)
        {
            this.m_Name = mmfName;
            Reinit();
        }
        private void Reinit()
        {
            accessor?.Dispose();
            mmf?.Dispose();
			mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.Read);
            accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<ShareInfo>(), MemoryMappedFileAccess.Read);
		}

		public bool TryRead(out ShareInfo info)
        {
            try
            {
                if (accessor == null)
                {
                    Reinit();
                }
                info = new ShareInfo(accessor);
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

    public struct OverlayConfig
    {
        public String CameraId;
        public Rectangle Bounds;
    }
}