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

		public GxMotionKey Add(string path, bool isLoop, float duration)
		{
			var duplicate = false;
			var clipInfo = new TimelineData(path, isLoop, duration);
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

		public IEnumerable<GxPoseData> GetPoseByTag(string[] tag)
		{
			if (tag == null || tag.Length == 0)
			{
				yield break; // No tags provided, nothing to return
			}

			foreach (var p in m_Poses)
			{
				if (p.Tags == null || p.Tags.Count == 0)
					continue;
				foreach (var t in tag)
				{
					if (p.ContainTags(tag) > 0)
					{
						yield return p;
						break;
					}
				}
			}
		}

		public bool TryGetRandomPoseByTags(string[] tag, out GxPoseData pose)
		{
			if (tag == null || tag.Length == 0)
			{
				pose = default;
				return false; // No tags provided, cannot find a pose
			}
			var filtered = GetPoseByTag(tag).ToArray();
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