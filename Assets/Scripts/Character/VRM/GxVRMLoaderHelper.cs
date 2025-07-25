using Kit2;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniGLTF;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
	[System.Obsolete("Test script during development.", true)]
    public class GxVRMLoaderHelper : MonoBehaviour
    {
		[Header("StreamingAssets File")]
		[SerializeField] private string m_ModelPath = $"AliciaSolid_vrm-0.51.vrm";
        [SerializeField] private string m_VRMAPath = "VRMA_01.vrma";
		private void OnEnable()
		{
            // LoadVRM();
		}

		[System.Obsolete("Use GxWinCharacter instead, that support GxWin container spawn flow.", true)]
		[ContextMenu("Load VRM")]
		private void LoadVRM()
        {
			var path = KxPath.Combine(GxConst.Path.VRM, m_ModelPath);
			_ = GxVRMLoader.LoadModel(path, OnModelLoaded, Debug.LogException);
		}

		#region Charcter
		private GxCharacter m_Character;
		private void OnModelLoaded(GxCharacter character, Vrm10Instance vrm)
		{
			m_Character = character;
			character.transform.SetParent(transform, false);
		}
		#endregion Charcter

		#region VRMA
		private RuntimeGltfInstance m_Gltf;
        private Vrm10AnimationInstance m_Vrma;

		[ContextMenu("Load VRMA")]
		[System.Obsolete("Use GxCharacter.CrossFade instead.", true)]
		private void LoadAnimation()
        {
			if (m_Character == null)
			{
				Debug.LogError("Character not exist. abort request.");
				return;
			}
			var path = KxPath.Combine(Application.streamingAssetsPath, m_VRMAPath);
			var key = new GxMotionKey(path, eAssetType.VRMA);
			m_Character.CrossFade(key, 0.25f);
		}
		[System.Obsolete("Use GxCharacter.CrossFade instead.", true)]
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
		[System.Obsolete("Use GxCharacter.CrossFade instead.", true)]
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