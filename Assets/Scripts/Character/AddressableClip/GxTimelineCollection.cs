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
		const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
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

		[SerializeField] private List<GxTimelineData> m_Timelines = new List<GxTimelineData>();

		[SerializeField] private List<GxPoseData> m_Poses = new List<GxPoseData>();

		public IReadOnlyList<GxTimelineData> Timelines => m_Timelines;
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

		#region Timeline Session
		public int MotionCount()
		{
			return m_Timelines != null ? m_Timelines.Count : 0;
		}

		public GxTimelineData GetMotionAt(int index)
		{
			if (index < 0 || index >= m_Timelines.Count)
				throw new System.IndexOutOfRangeException();
			return m_Timelines[index];
		}

		public void Add(GxTimelineData data)
		{
			var path = data.Path;
			foreach(var tl in m_Timelines)
			{
				if (!tl.Path.Equals(path, IGNORE))
					continue;
				return;
			}
			m_Timelines.Add(data);
		}
		#endregion Timeline Session

		#region Pose Session
		public bool Add(GxPoseData data)
		{
			var key = data.key;
			foreach (var p in m_Poses)
			{
				if (!p.key.Equals(key, IGNORE))
					continue;
				return false;
			}
			m_Poses.Add(data);
			return true;
		}
		public int PoseCount()
		{
			return m_Poses != null ? m_Poses.Count : 0;
		}

		public GxPoseData GetPoseAt(int index)
		{
			if (index < 0 || index >= m_Poses.Count)
				throw new System.IndexOutOfRangeException();
			return m_Poses[index];
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
		#endregion Pose Session
	}
}