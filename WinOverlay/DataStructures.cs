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
	/// Handle shared memory for GPU texture information.
	/// </summary>
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
	/// Handle shared memory for Camera MVP data.
	/// </summary>
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



	public struct OverlayConfig
    {
        public String CameraId;
        public Rectangle Bounds;
    }
}