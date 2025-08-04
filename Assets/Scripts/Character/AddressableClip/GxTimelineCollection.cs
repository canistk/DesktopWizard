using Kit2;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace Gaia
{
	/// <summary>
	/// A collection of timeline names that can be used to reference timelines in the Gaia system.
	/// </summary>
	/// <remarks>
	/// This class is used to store a list of timeline names that can be referenced by other systems in Gaia.
	/// </remarks>
	[CreateAssetMenu(fileName = "GxTimelineCollection", menuName = "Gaia/GxTimelineCollection", order = 1)]
	public class GxTimelineCollection : ScriptableObject
    {
		private static KeyValuePair<bool, GxTimelineCollection> m_Instance = default;
		public static GxTimelineCollection Instance
		{
			get
			{
				if (!m_Instance.Key)
				{
					var obj = Resources.Load<GxTimelineCollection>("GxTimelineCollection");
					if (obj == null)
					{
						Debug.LogError("GxTimelineCollection not found in Resources. Please ensure it exists.");
					}
					m_Instance = new KeyValuePair<bool, GxTimelineCollection>(true, obj);
				}
				return m_Instance.Value;
			}
		}
		[RuntimeInitializeOnLoadMethod]
		private static void AutoBind()
		{
			ReferenceEquals(Instance, null);
		}

		[SerializeField] private List<TimelineData> m_Timelines = new List<TimelineData>();

		[SerializeField] private List<GxPoseData> m_Poses = new List<GxPoseData>();

		[System.Serializable]
		public class TimelineData : GxMotionData
		{
			public TimelineData(string address, bool isLoop, float duration)
				: base()
			{
				this.Key = new GxMotionKey(address, eAssetType.Timeline);
				this.IsLoop = isLoop;
				this.ClipLength = duration;
				this.Weight = 1.0f; // Default weight
			}
		}

		public IReadOnlyList<TimelineData> Timelines => m_Timelines;

		public int MotionCount()
		{
			return m_Timelines != null ? m_Timelines.Count : 0;
		}

		public GxMotionKey Add(string path, bool isLoop, float duration, out TimelineData clipInfo)
		{
			var duplicate = false;
			clipInfo = new TimelineData(path, isLoop, duration);
			for (int i = 0; i < m_Timelines.Count; ++i)
			{
				var rec = m_Timelines[i];
				if (rec.Path == path)
				{
					duplicate = true;
					Debug.LogWarning($"Timeline with path '{path}' already exists in the collection. Skipping addition.");
					m_Timelines[i] = clipInfo;
				}
			}
			if (!duplicate)
			{
				m_Timelines.Add(clipInfo);
			}
			return clipInfo.Key;
		}

		public string AddPose(GxPoseData pose)
		{
			m_Poses.Add(pose);
			return pose.key;
		}

		public int PoseCount()
		{
			return m_Poses != null ? m_Poses.Count : 0;
		}

		public bool TryGetRandomPose(out GxPoseData pose)
		{
			pose = default;
			if (m_Poses == null || m_Poses.Count == 0)
			{
				Debug.LogWarning("No poses available in the collection.");
				return false;
			}
			var rnd = Random.Range(0, m_Poses.Count);
			pose = m_Poses[rnd];
			return pose != null;
		}

		public void Clear()
		{
			if (m_Timelines != null)
			{
				m_Timelines.Clear();
			}
			if (m_Poses != null)
			{
				m_Poses.Clear();
			}
		}

		public bool TryGetPose(string key, out GxPoseData pose)
		{
			foreach (var p in m_Poses)
			{
				if (!key.Equals(p.key))
					continue;
				pose = p;
				return true;
			}
			pose = default;
			return false;
		}

		public IReadOnlyList<GxPoseData> Poses
		{
			get
			{
				return m_Poses;
			}
		}

		/// <summary>Return poses that contain the specified tags.</summary>
		/// <param name="includeTag">tags should included from the result</param>
		/// <param name="excludeTag">tags to exclude from the result.</param>
		/// <param name="minCount">how many tag pass the test ? (for IncludeTag only)</param>
		/// <returns></returns>
		public IEnumerable<GxPoseData> GetPoseByTag(string[] includeTag, string[] excludeTag, int minCount)
		{
			var noIncludeTags = includeTag == null || includeTag.Length == 0 || (includeTag.Length == 1 && includeTag[0].Length == 0);
			var noExcludeTags = excludeTag == null || excludeTag.Length == 0 || (excludeTag.Length == 1 && excludeTag[0].Length == 0);
			if (noIncludeTags && noExcludeTags)
			{
				yield break; // No tags provided, nothing to return
			}
			if (minCount < 0)
			{
				minCount = 0; // Ensure minCount is not negative
			}

			foreach (var p in m_Poses)
			{
				if (p.Tags == null || p.Tags.Count == 0)
					continue;
				if (!noExcludeTags && p.ContainTags(excludeTag) > 0)
					continue; // Skip if any exclude tag is present

				if (noIncludeTags || p.ContainTags(includeTag) >= minCount)
					yield return p;
			}
		}

		public bool TryGetRandomPoseByTags(string[] includeTags, string[] excludeTags, int minCnt, out GxPoseData pose)
		{
			var noIncludeTags = includeTags == null || includeTags.Length == 0;
			var noExcludeTags = excludeTags == null || excludeTags.Length == 0;
			if (noIncludeTags && noExcludeTags)
			{
				pose = default;
				return false; // No tags provided, cannot find a pose
			}
			var filtered = GetPoseByTag(includeTags, excludeTags, minCnt).ToArray();
			if (filtered.Length == 0)
			{
				pose = default;
				return false; // No matching poses found
			}

			var rnd = Random.Range(0, filtered.Length);
			pose = filtered[rnd];
			return pose != null; // No matching pose found
		}
	}
}