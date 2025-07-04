using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using Unity.VisualScripting;
using UniGLTF;
using UniVRM10;
namespace Gaia

{
    [RequireComponent(typeof(GxCharacter))]
	public class GxTimelineHelper : MonoBehaviour
    {
        [SerializeField] private GxTimelineCollection m_Database;
        public GxTimelineCollection db => m_Database;

        private GxCharacter m_Character;
        public GxCharacter character
        {
            get
            {
                if (m_Character == null)
                {
                    m_Character = GetComponent<GxCharacter>();
				}
                return m_Character;
			}
        }

        [Header("Addressable, Animation Clips")]
        public int m_FirstIndex = 0;
        public int m_SecondIndex = 0;

        public float m_FadeIn = 0.2f;

        [Header("VRMA, load path")]
		[SerializeField] public string m_VRMAPath = "Assets/StreamingAssets/VRMA_01.vrma";
		


		private void Reset()
		{
            m_Database = Resources.Load<GxTimelineCollection>("GxTimelineCollection");
		}

		private void Awake()
		{
			if (m_Database == null)
            {
				m_Database = Resources.Load<GxTimelineCollection>("GxTimelineCollection");
			}
		}

		public void Editor_AnimationClip(int idx)
		{
#if UNITY_EDITOR
			var r = db.Timelines[idx];
			character.CrossFade(r.addressPath, m_FadeIn);
#endif
		}

		#region VRMA
		public void Editor_LoadVRMA(string path)
		{
#if UNITY_EDITOR
			LoadVRMA(path, OnVRMALoaded, Debug.LogException);
#endif
		}

		private RuntimeGltfInstance m_Gltf;
		private Vrm10AnimationInstance m_Vrma;

		public void LoadVRMA(string path,
			System.Action<RuntimeGltfInstance> loaded,
			System.Action<System.Exception> fail = null)
		{
			InternalLoadVRMA(path, OnVRMALoaded, fail);
		}

		private async void InternalLoadVRMA(string path,
			System.Action<RuntimeGltfInstance> loaded,
			System.Action<System.Exception> fail = null)
		{
			try
			{
				if (string.IsNullOrEmpty(path))
					throw new System.ArgumentNullException(nameof(path), "VRMA path cannot be null or empty.");
				using (GltfData data = new AutoGltfFileParser(path).Parse())
				using (var loader = new VrmAnimationImporter(new VrmAnimationData(data)))
				{
					var instance = await loader.LoadAsync(new ImmediateCaller());
					loaded?.Invoke(instance);
				}
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
			this.m_Gltf.ShowMeshes();

			this.m_Vrma = gltf.GetComponent<Vrm10AnimationInstance>();
			if (this.m_Vrma)
			{
				this.m_Vrma.ShowBoxMan(true);
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