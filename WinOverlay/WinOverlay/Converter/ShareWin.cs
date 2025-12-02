using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

namespace WinOverlay
{

	/// <summary>Protobuf class for keyboard event data.
	/// From WinOverlay to KawaiOS</summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct KeyboardEventP3
	{
		public const string LABEL = "KEY_";
		public static readonly byte[] labelBytes = System.Text.Encoding.UTF8.GetBytes(LABEL);
		public int KeyCode;
		public bool Alt;
		public bool Control;
		public bool Shift;
		public bool Handled;
		public bool SuppressKeyPress;
		public bool IsKeyUp;

		public KeyboardEventP3(KeyEventArgs e, bool isKeyUp)
		{
			KeyCode = (int)e.KeyCode;
			Alt = e.Alt;
			Control = e.Control;
			Shift = e.Shift;
			Handled = e.Handled;
			SuppressKeyPress = e.SuppressKeyPress;
			IsKeyUp = isKeyUp;
		}

		public byte[] ToByteArray()
		{
			int size = Marshal.SizeOf(this);
			byte[] arr = new byte[size + 4];
			// First 4 bytes for label
			Buffer.BlockCopy(labelBytes, 0, arr, 0, 4);

			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(this, ptr, true);
			Marshal.Copy(ptr, arr, 4, size);
			Marshal.FreeHGlobal(ptr);
			return arr;
		}

		public static KeyboardEventP3 FromByteArray(ref byte[] arr)
		{
			if (!IsValid(ref arr))
				throw new ArgumentException("Byte array does not contain valid KeyboardEventP3 data.");

			KeyboardEventP3 str = new KeyboardEventP3();
			int size = Marshal.SizeOf(str);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(arr, 4, ptr, size); // Skip the first 4 bytes (labelBytes)
			str = (KeyboardEventP3)Marshal.PtrToStructure(ptr, str.GetType());
			Marshal.FreeHGlobal(ptr);
			return str;
		}

		public static bool IsValid(ref byte[] bytes)
		{
			var span = new ReadOnlySpan<byte>(bytes, 0, 4);
			return span.SequenceEqual(labelBytes);
		}
	}

	/// <summary>Protobuf class for mouse event data.
	/// From WinOverlay to KawaiOS</summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MouseEventP3
	{
		public const string LABEL = "MOU_";
		public static readonly byte[] labelBytes = System.Text.Encoding.UTF8.GetBytes(LABEL);
		public int Button;
		public int Clicks;
		public int X;
		public int Y;
		public int Delta;
		public MouseEventP3(MouseEventArgs e)
		{
			Button = (int)e.Button;
			Clicks = e.Clicks;
			X = e.X;
			Y = e.Y;
			Delta = e.Delta;
		}

		public byte[] ToByteArray()
		{
			int size = Marshal.SizeOf(this);
			byte[] arr = new byte[size + 4];
			// First 4 bytes for label
			Buffer.BlockCopy(labelBytes, 0, arr, 0, 4);

			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(this, ptr, true);
			Marshal.Copy(ptr, arr, 4, size);
			Marshal.FreeHGlobal(ptr);
			return arr;
		}

		public static MouseEventP3 FromByteArray(byte[] arr)
		{
			if (!IsValid(ref arr))
				throw new ArgumentException("Byte array does not contain valid MouseEventP3 data.");

			MouseEventP3 str = new MouseEventP3();
			int size = Marshal.SizeOf(str);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(arr, 4, ptr, size); // Skip the first 4 bytes (labelBytes)
			str = (MouseEventP3)Marshal.PtrToStructure(ptr, str.GetType());
			Marshal.FreeHGlobal(ptr);
			return str;
		}

		public static bool IsValid(ref byte[] bytes)
		{
			var span = new ReadOnlySpan<byte>(bytes, 0, 4);
			return span.SequenceEqual(labelBytes);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct CameraInfo
	{
		public const string LABEL = "CAM_";
		public static readonly byte[] labelBytes = System.Text.Encoding.UTF8.GetBytes(LABEL);
		public static readonly int ByteArraySize = 
			4 + // label
			16 * 4 + // O2M
			16 * 4 + // M2F
			2 * 4 + // OsPos
			3 * 4 + // MonPos
			2 * 4 + // FormOSPos
			3 * 4; // FormPos

		public Mat4x4 O2M;
		public Mat4x4 M2F;
		public Vec2Int OsPos;
		public Vec3 MonPos;
		public Vec2Int FormOSPos;
		public Vec3 FormPos;
		public CameraInfo(Mat4x4 o2m, Mat4x4 m2f, Vec2Int osPos, Vec3 monPos, Vec2Int formOSPos, Vec3 formPos)
		{
			this.O2M = o2m;
			this.M2F = m2f;
			this.OsPos = osPos;
			this.FormOSPos = formOSPos;
			this.MonPos = monPos;
			this.FormPos = formPos;
		}

		public byte[] ToByteArray()
		{
			List<byte> byteList = new List<byte>();

			byteList.AddRange(labelBytes);

			// Serialize O2M
			foreach (var f in O2M.M)
				byteList.AddRange(BitConverter.GetBytes(f));
			// Serialize M2F
			foreach (var f in M2F.M)
				byteList.AddRange(BitConverter.GetBytes(f));
			// Serialize OsPos
			byteList.AddRange(BitConverter.GetBytes(OsPos.X));
			byteList.AddRange(BitConverter.GetBytes(OsPos.Y));
			// Serialize MonPos
			byteList.AddRange(BitConverter.GetBytes(MonPos.X));
			byteList.AddRange(BitConverter.GetBytes(MonPos.Y));
			byteList.AddRange(BitConverter.GetBytes(MonPos.Z));
			// Serialize FormOSPos
			byteList.AddRange(BitConverter.GetBytes(FormOSPos.X));
			byteList.AddRange(BitConverter.GetBytes(FormOSPos.Y));
			// Serialize FormPos
			byteList.AddRange(BitConverter.GetBytes(FormPos.X));
			byteList.AddRange(BitConverter.GetBytes(FormPos.Y));
			byteList.AddRange(BitConverter.GetBytes(FormPos.Z));

			return byteList.ToArray();
		}

		public static bool IsValid(ref byte[] bytes)
		{
			var span = new ReadOnlySpan<byte>(bytes, 0, 4);
			return span.SequenceEqual(labelBytes);
		}

		public static CameraInfo FromByteArray(ref byte[] bytes)
		{
			if (!IsValid(ref bytes))
				throw new ArgumentException("Byte array does not contain valid CameraInfo data.");
			using (var ms = new MemoryStream(bytes))
			using (var br = new BinaryReader(ms))
			{
				// skip label
				br.ReadBytes(4);
				// Read O2M
				float[] o2m = new float[16];
				for (int i = 0; i < 16; i++) o2m[i] = br.ReadSingle();
				// Read M2F
				float[] m2f = new float[16];
				for (int i = 0; i < 16; i++) m2f[i] = br.ReadSingle();
				// Read OsPos
				int osPosX = br.ReadInt32();
				int osPosY = br.ReadInt32();
				// Read MonPos
				float monPosX = br.ReadSingle();
				float monPosY = br.ReadSingle();
				float monPosZ = br.ReadSingle();

				// Read FormOSPos
				int formOSPosX = br.ReadInt32();
				int formOSPosY = br.ReadInt32();
				// Read FormPos
				float formPosX = br.ReadSingle();
				float formPosY = br.ReadSingle();
				float formPosZ = br.ReadSingle();

				return new CameraInfo(
					new Mat4x4(o2m),
					new Mat4x4(m2f),
					new Vec2Int(osPosX, osPosY),
					new Vec3(monPosX, monPosY, monPosZ),
					new Vec2Int(formOSPosX, formOSPosY),
					new Vec3(formPosX, formPosY, formPosZ)
				);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct TextureInfo
	{
		public IntPtr rtHandler;
		public DateTime timestamp;
		public int width;
		public int height;
		public int rowPitch;
		public int bytesPerPixel;
		public int totalSize;
		public float chromeKeyR;
		public float chromeKeyG;
		public float chromeKeyB;
		public float chromeRange;
		public bool useChromeKey;

		//public TextureInfo(RenderTexture renderTexture, int totalSize, Color chromeKey, float chromeRange, bool useChromaKey)
		//{
		//	this.rtHandler = renderTexture.GetNativeTexturePtr();
		//	this.timestamp = DateTime.UtcNow;
		//	this.width = renderTexture.width;
		//	this.height = renderTexture.height;
		//	this.bytesPerPixel = 4; // RGBA32
		//	this.rowPitch = renderTexture.width * 4;
		//	this.totalSize = totalSize;
		//	this.chromeKeyR = chromeKey.r;
		//	this.chromeKeyG = chromeKey.g;
		//	this.chromeKeyB = chromeKey.b;
		//	this.chromeRange = chromeRange;
		//	this.useChromeKey = useChromaKey;
		//}

		public TextureInfo(MemoryMappedViewAccessor accessor)
		{
			rtHandler = (IntPtr)accessor.ReadInt64(0);
			timestamp = DateTime.FromBinary(accessor.ReadInt64(8));
			width = accessor.ReadInt32(16);
			height = accessor.ReadInt32(20);
			rowPitch = accessor.ReadInt32(24);
			bytesPerPixel = accessor.ReadInt32(28);
			totalSize = accessor.ReadInt32(32);
			chromeKeyR = accessor.ReadSingle(36);
			chromeKeyG = accessor.ReadSingle(40);
			chromeKeyB = accessor.ReadSingle(44);
			chromeRange = accessor.ReadSingle(48);
			useChromeKey = accessor.ReadBoolean(52);
		}

		public void GetChromeKeyColor(out Int32 r, out Int32 g, out Int32 b, out float range01)
		{
			r = (Int32)(chromeKeyR * 255);
			g = (Int32)(chromeKeyG * 255);
			b = (Int32)(chromeKeyB * 255);
			range01 = chromeRange * 255;
			if (range01 < 0) range01 = 0;
			if (range01 > 255) range01 = 255;
		}

		public static DateTime FetchDatetime(MemoryMappedViewAccessor accessor)
		{
			return DateTime.FromBinary(accessor.ReadInt64(8));
		}
	}
}
