using Kit2;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
  
		[System.Serializable]
		public class TimelineData : GxMotionData
		{
			[JsonIgnore]
			public AssetReference assetRef;
			
			public TimelineData(AssetReference assetRef, string address, AnimationClip clip)
				: this(assetRef, address, clip.isLooping, clip.length)
			{ }

			public TimelineData(AssetReference assetRef, string address, bool isLoop, float duration)
				: base()
			{
				this.Type = eAssetType.Timeline;
				this.Path = address;
				this.assetRef = assetRef;
				this.IsLoop = isLoop;
				this.ClipLength = duration;
				this.Weight = 1.0f; // Default weight
			}
		}

		public IReadOnlyList<TimelineData> Timelines => m_Timelines;

		public int Count()
		{
			return m_Timelines != null ? m_Timelines.Count : 0;
		}

		public void Add(AssetReference assetRef, string path, AnimationClip clip)
		{
			var duplicate = false;
			var clipInfo = new TimelineData(assetRef, path, clip);
			for (int i = 0; i < m_Timelines.Count; ++i)
			{
				var rec = m_Timelines[i];
				if (rec.Path == path)
				{
					duplicate = true;
					Debug.LogWarning($"Timeline with path '{path}' already exists in the collection. Skipping addition.");
					m_Timelines[i] = clipInfo;
					return;
				}
			}
			if (!duplicate)
			{
				m_Timelines.Add(clipInfo);
			}
		}

		public void Clear()
		{
			if (m_Timelines == null)
				return;
			m_Timelines.Clear();
		}
    }
}