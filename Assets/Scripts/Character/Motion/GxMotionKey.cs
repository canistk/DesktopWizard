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
		public string ShortName => KxPath.GetFileNameWithoutExtension(Path);
		public string Path;
		public eAssetType Type;
		public bool Valid;
		public GxMotionKey(string path, eAssetType type)
		{
			Path = path;
			Type = type;
			Valid = true;
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
				return Path == key.Path && Type == key.Type;
			}
			if (obj is GxMotionData data)
			{
				return Path == data.Path && Type == data.Type;
			}
			return false;
		}

		public bool Equals(GxMotionKey x, GxMotionKey y)
		{
			return x.Path == y.Path && x.Type == y.Type;
		}

		public bool Equals(GxMotionData other)
		{
			return Path == other.Path && Type == other.Type;
		}

		public bool Equals(GxMotionKey other)
		{
			return Path == other.Path && Type == other.Type;
		}
	}

}