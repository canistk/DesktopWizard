using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.EventSystems;
#endif
namespace Share
{

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
}
