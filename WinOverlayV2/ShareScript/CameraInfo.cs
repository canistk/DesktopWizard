using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
#endif
namespace Share
{
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
			if (bytes == null || bytes.Length < 4)
				return false;
			for (int i = 0; i < 4; i++)
			{
				if (bytes[i] != labelBytes[i])
					return false;
			}
			return true;
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

}
