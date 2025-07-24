using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using Unity.VisualScripting;
using UniGLTF;
using UniVRM10;
using System.Linq;
namespace Gaia
{
	using DB = GxMotionDatabase;
	[RequireComponent(typeof(GxCharacter))]
	public class GxMotionHelper : MonoBehaviour
    {
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
		GxMotionKey[] m_Data;
		public GxMotionKey[] data
		{
			get
			{
				if (m_Data == null)
				{
					m_Data = DB.GetMotions().Select(o => o.Key).ToArray();
				}
				return m_Data;
			}
		}

		private void Reset()
		{
		}

		private void Awake()
		{
			
		}

		public void Editor_AnimationClip(int idx)
		{
#if UNITY_EDITOR
			if (idx < 0 || idx >= data.Length)
			{
				Debug.LogWarning($"Invalid index {idx}. Must be between 0 and {data.Length - 1}.");
				return;
			}
			var o = data[idx];
			character.CrossFade(o, m_FadeIn, true);
			// character.CrossFade(o.Path, m_FadeIn, Kit2.ObjectPool.eSrcType.Addressable);
#endif
		}

		#region VRMA
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