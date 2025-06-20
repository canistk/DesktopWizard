using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private List<ClipInfo> m_Timelines = new List<ClipInfo>();
  
		[System.Serializable]
		private struct ClipInfo
		{
			public string path;
			public bool isLoop;
			public float duration;

			public ClipInfo(AnimationClip clip, string path)
			{
   				this.path = path;
				this.isLoop = clip.isLooping;
				this.duration = clip.length;
			}

			public ClipInfo(string path, bool isLoop, float duration)
			{
				this.path = path;
				this.isLoop = isLoop;
				this.duration = duration;
			}
		}

		public void Add(string path, AnimationClip clip)
		{
			var duplicate = false;
			for (int i = 0; i < m_Timelines.Count; ++i)
			{
				var rec = m_Timelines[i];
				if (rec.path == path)
				{
					duplicate = true;
					Debug.LogWarning($"Timeline with path '{path}' already exists in the collection. Skipping addition.");
					m_Timelines[i] = new ClipInfo(clip, path);
					return;
				}
			}
			if (!duplicate)
			{
				m_Timelines.Add(new ClipInfo(clip, path));
			}
		}
    }
}