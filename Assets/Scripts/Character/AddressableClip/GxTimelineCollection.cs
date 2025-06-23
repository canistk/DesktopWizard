using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
		public struct ClipInfo
		{
			public string addressPath;
			public AssetReference assetRef;
			public bool isLoop;
			public float duration;

			public ClipInfo(AssetReference assetRef, string address, AnimationClip clip)
				: this(assetRef, address, clip.isLooping, clip.length)
			{ }

			public ClipInfo(AssetReference assetRef, string address, bool isLoop, float duration)
			{
				this.addressPath = address;
				this.assetRef = assetRef;
				this.isLoop = isLoop;
				this.duration = duration;
			}
		}
		public IReadOnlyList<ClipInfo> Timelines => m_Timelines;

		public void Add(AssetReference assetRef, string path, AnimationClip clip)
		{
			var duplicate = false;
			var clipInfo = new ClipInfo(assetRef, path, clip);
			for (int i = 0; i < m_Timelines.Count; ++i)
			{
				var rec = m_Timelines[i];
				if (rec.addressPath == path)
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
    }
}