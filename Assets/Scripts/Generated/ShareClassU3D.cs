using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Share
{
	public interface IInputEvent { }
	/// <summary>Protobuf class for keyboard event data.
	/// From WinOverlay to KawaiOS</summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct KeyboardEventP3 : IInputEvent
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
	public struct MouseEventP3 : IInputEvent
	{
		public const string LABEL = "MOU_";
		public static readonly byte[] labelBytes = System.Text.Encoding.UTF8.GetBytes(LABEL);
		public int Button;
		public int State; // 0 : down, 1: up, 2: move, 3: wheel
		public int Clicks;
		public int X;
		public int Y;
		public int WheelDelta;
		public float monX;
		public float monY;
		public float formX;
		public float formY;
		public bool withinForm;

#if !(UNITY_EDITOR || UNITY_STANDALONE)
		public MouseEventP3(int state, MouseEventArgs e, CameraInfo cam, bool withinForm)
		{
			Button = (int)e.Button;
			State = state;
			if (state < 0 || state > 3)
				throw new ArgumentOutOfRangeException("state", "State must be 0 (down), 1 (up), 2 (move), or 3 (wheel).");
			Clicks = e.Clicks;
			X = e.X;
			Y = e.Y;
			WheelDelta = e.Delta;
			var monPos = cam.O2M.MultiplyPoint3x4(new Vec3(X, Y, 0));
			monX = monPos.X;
			monY = monPos.Y;

			var formPos = cam.M2F.MultiplyPoint3x4(monPos);
			formX = formPos.X;
			formY = formPos.Y;

			this.withinForm = withinForm;
		}
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
		public PointerEventData ConvertToPointerEventData()
		{
			//var osPos = new Vector2(X, Y);
			//var formPos = new Vector2(formX, formY);
			var monPos = new Vector2(monX, monY);
			PointerEventData ped = new PointerEventData(EventSystem.current)
			{
				button = (PointerEventData.InputButton)Button,
				position = monPos,
				pressPosition = State == 0 ? monPos : Vector2.zero,
				clickCount = Clicks,
				scrollDelta = new Vector2(0, WheelDelta),
				// Rest require calculate in system.
			};
			return ped;
		}
#endif

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

		public static MouseEventP3 FromByteArray(ref byte[] arr)
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
			this.MonPos = monPos;

			this.FormOSPos = formOSPos;
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
		public DateTime timestamp;

#if UNITY_EDITOR || UNITY_STANDALONE
		public TextureInfo(RenderTexture renderTexture, int totalSize, Color chromeKey, float chromeRange, bool useChromaKey)
		{
			this.rtHandler = renderTexture.GetNativeTexturePtr();
			this.width = renderTexture.width;
			this.height = renderTexture.height;
			this.bytesPerPixel = 4; // RGBA32
			this.rowPitch = renderTexture.width * 4;
			this.totalSize = totalSize;
			this.chromeKeyR = chromeKey.r;
			this.chromeKeyG = chromeKey.g;
			this.chromeKeyB = chromeKey.b;
			this.chromeRange = chromeRange;
			this.useChromeKey = useChromaKey;
			this.timestamp = DateTime.UtcNow;
		}
#endif

		public TextureInfo(MemoryMappedViewAccessor accessor)
		{
			var i = 0;
			rtHandler = (IntPtr)accessor.ReadInt64(0); i += 8;
			width = accessor.ReadInt32(i); i += 4;
			height = accessor.ReadInt32(i); i += 4;
			rowPitch = accessor.ReadInt32(i); i += 4;
			bytesPerPixel = accessor.ReadInt32(i); i += 4;
			totalSize = accessor.ReadInt32(i); i += 4;
			chromeKeyR = accessor.ReadSingle(i); i += 4;
			chromeKeyG = accessor.ReadSingle(i); i += 4;
			chromeKeyB = accessor.ReadSingle(i); i += 4;
			chromeRange = accessor.ReadSingle(i); i += 4;
			useChromeKey = accessor.ReadBoolean(i); i += 1;
			timestamp = DateTime.FromBinary(accessor.ReadInt64(i)); i += 8;
		}

		public void WriteToAccessor(MemoryMappedViewAccessor accessor)
		{
			var i = 0;
			accessor.Write(i, (long)rtHandler); i += 8;
			accessor.Write(i, width); i += 4;
			accessor.Write(i, height); i += 4;
			accessor.Write(i, rowPitch); i += 4;
			accessor.Write(i, bytesPerPixel); i += 4;
			accessor.Write(i, totalSize); i += 4;
			accessor.Write(i, chromeKeyR); i += 4;
			accessor.Write(i, chromeKeyG); i += 4;
			accessor.Write(i, chromeKeyB); i += 4;
			accessor.Write(i, chromeRange); i += 4;
			accessor.Write(i, useChromeKey); i += 1;
			accessor.Write(i, timestamp.ToBinary()); i += 8;
		}
		public static DateTime FetchDatetime(MemoryMappedViewAccessor accessor)
		{
			// Ensure last 8 bytes are writen into timestamp
			return DateTime.FromBinary(accessor.ReadInt64(45));
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
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct Mat4x4
	{
		public float[] M; // 16 elements
		public Mat4x4(float[] elements)
		{
			if (elements.Length != 16)
				throw new ArgumentException("Mat4x4 requires exactly 16 elements.");
			M = elements;
		}
		public Matrix4x4 ToMatrix4x4()
		{
			return new Matrix4x4(
				new Vector4(M[ 0], M[ 1], M[ 2], M[ 3]),
				new Vector4(M[ 4], M[ 5], M[ 6], M[ 7]),
				new Vector4(M[ 8], M[ 9], M[10], M[11]),
				new Vector4(M[12], M[13], M[14], M[15])
			);
		}

		public static implicit operator Matrix4x4(Mat4x4 mat) => mat.ToMatrix4x4();
		public static implicit operator Mat4x4(Matrix4x4 mat)
		{
			return new Mat4x4(
				new float[]
				{
					mat.m00, mat.m01, mat.m02, mat.m03,
					mat.m10, mat.m11, mat.m12, mat.m13,
					mat.m20, mat.m21, mat.m22, mat.m23,
					mat.m30, mat.m31, mat.m32, mat.m33
				});
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vec3
	{
		public float X,Y,Z;
		public Vec3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}
		public Vector3 ToVector3() => new Vector3(X, Y, Z);
		public static implicit operator Vector3(Vec3 vec) => vec.ToVector3();
		public static implicit operator Vec3(Vector3 vec)
		{
			return new Vec3
			{
				X = vec.x,
				Y = vec.y,
				Z = vec.z
			};
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vec2
	{
		public float X,Y;
		public Vec2(float x, float y)
		{
			X = x;
			Y = y;
		}
		public Vector2 ToVector2() => new Vector2(X, Y);
		public static implicit operator Vector2(Vec2 vec) => vec.ToVector2();
		public static implicit operator Vec2(Vector2 vec)
		{
			return new Vec2
			{
				X = vec.x,
				Y = vec.y
			};
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vec2Int
	{
		public int X,Y;
		public Vec2Int(int x, int y)
		{
			X = x;
			Y = y;
		}
		public Vector2Int ToVector2Int() => new Vector2Int(X, Y);
		public static implicit operator Vector2Int(Vec2Int vec) => vec.ToVector2Int();
		public static implicit operator Vec2Int(Vector2Int vec)
		{
			return new Vec2Int
			{
				X = vec.x,
				Y = vec.y
			};
		}
	}

}