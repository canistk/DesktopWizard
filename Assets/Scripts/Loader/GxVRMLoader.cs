using Kit2;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniGLTF;
using Unity.VisualScripting;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
    public class GxVRMLoader : MonoBehaviour
    {
		[Header("StreamingAssets File")]
		[SerializeField] private string m_ModelPath = $"AliciaSolid_vrm-0.51.vrm";
        [SerializeField] private string m_VRMAPath = "VRMA_01.vrma";
		private void OnEnable()
		{
            LoadVRM();
		}

		[ContextMenu("Load VRM")]
		private void LoadVRM()
        {
            LoadModel(KxPath.Combine(Application.streamingAssetsPath,m_ModelPath), OnModelLoaded, Debug.LogException);
		}

		[ContextMenu("Streaming Assets")]
		private void GotoStreamingAssets()
		{
			Kit2.Platform.CommandLine("explorer.exe", KxPath.Fix(Application.streamingAssetsPath), (feedback) =>
			{
				Debug.Log(feedback);
			});
		}

		#region Charcter
		private GxCharacter m_Character;
		private async void LoadModel(string path,
			System.Action<Vrm10Instance> success,
			System.Action<System.Exception> fail = null)
		{
			try
			{
				if (string.IsNullOrEmpty(path))
				{
					Debug.LogError("VRM path is null or empty.");
					return;
				}
				var fileInfo = new FileInfo(path);
				if (!fileInfo.Exists)
				{
					Debug.LogError($"Invaild File {path}");
					return;
				}

				// Loaded VRM model
				var vrm = await Vrm10.LoadPathAsync(path, true);
				success?.Invoke(vrm);
			}
			catch (System.Exception ex)
			{
				if (fail == null)
				{
					Debug.LogException(ex);
					return;
				}
				fail.Invoke(ex);
			}
		}

		private void OnModelLoaded(Vrm10Instance vrm)
		{
			if (vrm == null)
			{
				Debug.LogError("VRM instance is null after loading.");
				return;
			}
			vrm.transform.SetParent(transform, false);
			
			// post loaded for character setup, Get the character component
			m_Character = vrm.GetOrAddComponent<GxCharacter>();
			m_Character.RuntimeCreation();
		}
		#endregion Charcter

		#region VRMA
		private RuntimeGltfInstance m_Gltf;
        private Vrm10AnimationInstance m_Vrma;

		[ContextMenu("Load VRMA")]
		private void LoadAnimation()
        {
            LoadVRMAFlow(KxPath.Combine(Application.streamingAssetsPath,m_VRMAPath), OnVRMALoaded, Debug.LogError);
		}
		private async void LoadVRMAFlow(string path,
			System.Action<RuntimeGltfInstance> loaded,
			System.Action<System.Exception> fail = null)
		{
			try
			{
				if (string.IsNullOrEmpty(m_VRMAPath))
					throw new System.ArgumentNullException(nameof(m_VRMAPath), "VRMA path cannot be null or empty.");
				VrmAnimationData vrmaData = default;
				using (GltfData data = new AutoGltfFileParser(path).Parse())
				{
					vrmaData = new VrmAnimationData(data);
				}
				using var loader = new VrmAnimationImporter(vrmaData);
				var instance = await loader.LoadAsync(new ImmediateCaller());
				loaded?.Invoke(instance);
			}
			catch (System.Exception ex)
			{
				if (fail == null)
				{
					Debug.LogException(ex);
					return;
				}
				fail.Invoke(ex);
			}
		}

		private void OnVRMALoaded(RuntimeGltfInstance gltf)
        {
			this.m_Gltf = gltf;
			this.m_Gltf.EnableUpdateWhenOffscreen();

			this.m_Vrma = gltf.GetComponent<Vrm10AnimationInstance>();
			if (this.m_Vrma)
			{
				this.m_Vrma.ShowBoxMan(false);
			}

			var animator = gltf.GetComponentInChildren<Animator>();
			if (animator)
			{
				var retargeting = animator.GetOrAddComponent<GxRetargeting>();
				retargeting.ForceTPose();

				//if (m_Character != null)
				//{
				//	m_Character.AddAnimationRetarget(retargeting);
				//}
			}

			var animation = gltf.GetComponent<Animation>();
			if (animation)
			{
				animation.Play();
			}
		}
		#endregion VRMA
	}
}