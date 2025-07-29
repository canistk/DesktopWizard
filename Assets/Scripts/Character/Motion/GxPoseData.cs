using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Gaia
{
	[System.Serializable]
	public class GxPoseData
	{
		public string key;

		/// <summary>
		/// Assume all tags are lowercase and trimmed.
		/// </summary>
		[SerializeField] List<string> m_Tags;
		public List<string> Tags
		{
			get
			{ 
				if (m_Tags == null)
				{
					m_Tags = new List<string>();
				}
				return m_Tags;
			}
			private set
			{
				m_Tags = value;
			}
		}
		[SerializeField] GxMotionKey[] m_EnterTracks;
		[SerializeField] GxMotionKey[] m_LoopTracks;
		[SerializeField] GxMotionKey[] m_ExitTracks;

		public GxPoseData(string key, GxMotionKey[] enter, GxMotionKey[] loop, GxMotionKey[] exit)
		{
			this.key = key;
			this.m_EnterTracks = enter;
			this.m_LoopTracks = loop;
			this.m_ExitTracks = exit;

			var tags = ConvertTags(key);
			foreach (var tag in tags)
			{
				if (!Tags.Contains(tag))
					Tags.Add(tag);
			}
		}

		static readonly string[] SKIP_TAG = {
				"start", "begin",
				"loop", "repeat", "mid",
				"end", "finish",
				"group"
			};
		private static readonly char[] SPLITS = new[]{ '_', '-', ' ' };
		private static List<string> ConvertTags(string str)
		{
			if (string.IsNullOrEmpty(str))
				return new List<string>(0);
			str = str.Trim().ToLowerInvariant();
			var arr = str.Split(SPLITS, System.StringSplitOptions.RemoveEmptyEntries);
			var tags = new List<string>(arr.Length);
			foreach (var tag in arr)
			{
				if (string.IsNullOrEmpty((string)tag))
					continue;
				if (SKIP_TAG.Contains(tag))
					continue; 
				tags.Add(tag);
			}
			return tags;
		}

		public GxPoseData(string key, GxMotionKey enter, GxMotionKey loop, GxMotionKey exit)
			: this(key, new[] { enter }, new[] { loop }, new[] { exit })
		{
		}
		public GxMotionKey[] GetEnterTracks() => m_EnterTracks;

		public GxMotionKey[] GetLoopTracks() => m_LoopTracks;

		public GxMotionKey[] GetExitTracks() => m_ExitTracks;

		public void AddTag(string tag)
		{
			if (string.IsNullOrEmpty(tag))
				return;
			var val = tag?.Trim().ToLowerInvariant();
			if (!Tags.Contains(val))
				Tags.Add(val); // ensure lowercase and trimmed
		}

		public void RemoveTag(string tag)
		{
			if (string.IsNullOrEmpty(tag))
				return;
			var val = tag?.Trim().ToLowerInvariant();
			if (Tags.Contains(tag))
				Tags.Remove(tag);
		}

		public int ContainTags(params string[] tags)
		{
			var match = 0;
			foreach (var tag in tags)
			{
				if (string.IsNullOrEmpty(tag))
					continue;
				var val = tag?.Trim().ToLowerInvariant();
				if (Tags.Contains(val))
					++match;
			}
			return match;
		}
	}
}
