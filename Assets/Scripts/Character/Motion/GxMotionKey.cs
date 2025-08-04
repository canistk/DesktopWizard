using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Kit2;
namespace Gaia
{
	public enum eEmotion : int
	{
		Unknown = 0,
		Neutral,
		Happy,
		Sadness,
		Anger,
		Frightened,
		Speechless,
		Sleep,
	}

	public enum eAssetType : int
	{
		Unknown = 0,
		VRMA, // VRM Animation
		Timeline, // Timeline Animation
	}

	[System.Serializable]
	public struct GxMotionKey :
		IEqualityComparer<GxMotionKey>,
		IEquatable<GxMotionKey>,
		IEquatable<GxMotionData>
	{
		public static readonly GxMotionKey Invalid = new GxMotionKey(null, eAssetType.Unknown);
		public string ShortName => KxPath.GetFileNameWithoutExtension(Path);
		public string Path;
		public eAssetType Type;
		public GxMotionKey(string path, eAssetType type)
		{
			Path = path;
			Type = type;
		}
		public override string ToString()
		{
			return $"{Type}:{Path}";
		}
		public override int GetHashCode()
		{
			return HashCode.Combine(Path, Type);
		}
		public int GetHashCode(GxMotionKey obj)
		{
			return HashCode.Combine(obj.Path, obj.Type);
		}

		public override bool Equals(object obj)
		{
			if (obj is GxMotionKey key)
			{
				return Type == key.Type && Path == key.Path;
			}
			if (obj is GxMotionData data)
			{
				return Type == data.Type && Path == data.Path;
			}
			return false;
		}

		public bool Equals(GxMotionKey x, GxMotionKey y)
		{
			return x.Type == y.Type && x.Path == y.Path;
		}

		public bool Equals(GxMotionData other)
		{
			return Type == other.Type && Path == other.Path;
		}

		public bool Equals(GxMotionKey other)
		{
			return Type == other.Type && Path == other.Path;
		}
	}

}