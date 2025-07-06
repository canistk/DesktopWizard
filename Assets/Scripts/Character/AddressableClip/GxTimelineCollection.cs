using Kit2;
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

			public async void LoadVRMA(System.Action<IVrm10Animation> vrma, System.Action<System.Exception> exception)
			{
				// EditorExtend.ResolvePath(path, out var absolutePath, out var relativePath);
				// vrma = Resources.Load(relativePath);
				// vrma = Resources.Load(relativePath);

				try
				{
					var path = $"{addressPath}_vrma.glb";
					var oper = Addressables.LoadAssetAsync<GameObject>(path);
					var task = await oper.Task;
					if (oper.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
					{
						var comp = oper.Result.GetComponent<IVrm10Animation>();
						if (comp == null)
							throw new System.Exception("VRMA not found.");
						var ani = comp as IVrm10Animation;
						if (ani == null)
							throw new System.Exception("VRMA not found.");
						vrma?.Invoke(ani);
					}
					else
					{
						Debug.LogError($"Failed to load VRMA from path: {path}");
						vrma?.Invoke(null);
					}
				}
				catch (System.Exception ex)
				{
					exception?.Invoke(ex);
				}
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