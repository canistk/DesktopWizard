using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using UniVRM10;
using UniGLTF;
using Kit2;
using Kit2.ObjectPool;
namespace Gaia
{
	/// <summary>
	/// A helper class for managing Gaia VRMA tokens in Unity.
	/// </summary>
	public class GxVRMAToken : MonoBehaviour, ISpawnToken, ISelfDespawnable
    {
		[SerializeField] private Animator m_Animator = null;
		public Animator Animator
		{
			get
			{
				if (m_Animator == null)
					m_Animator = GetComponent<Animator>();
				return m_Animator;
			}
		}

		[SerializeField] private Animation m_Animation = null;
		public Animation Animation
		{
			get
			{
				if (m_Animation == null)
					m_Animation = GetComponent<Animation>();
				return m_Animation;
			}
		}

		[SerializeField] private GxRetargeting m_Retargeting = null;
		public GxRetargeting Retargeting
		{
			get
			{
				if (m_Retargeting == null)
					m_Retargeting = this.Animator.gameObject.AddComponent<GxRetargeting>();
				return m_Retargeting;
			}
		}

		[SerializeField] private Vrm10AnimationInstance m_Vrm10AnimationInstance;
		public Vrm10AnimationInstance Vrm10AnimationInstance
		{
			get
			{
				if (m_Vrm10AnimationInstance == null)
					m_Vrm10AnimationInstance = GetComponent<Vrm10AnimationInstance>();
				return m_Vrm10AnimationInstance;
			}
		}

		[SerializeField] private RuntimeGltfInstance m_RuntimeGltfInstance = null;
		public RuntimeGltfInstance gltf
		{
			get
			{
				if (m_RuntimeGltfInstance == null)
					m_RuntimeGltfInstance = GetComponent<RuntimeGltfInstance>();
				return m_RuntimeGltfInstance;
			}
		}

		[SerializeField] GxMotionKey m_MotionKey;
		public GxMotionKey key => m_MotionKey;
		public void Setup(GxMotionKey key)
		{
			this.m_MotionKey = key;

			this.Animation.cullingType = AnimationCullingType.AlwaysAnimate;
			this.Vrm10AnimationInstance.ShowBoxMan(false);

			// Enfore T-Pose for the VRMA
			var cache = this.Animator.enabled;
			this.Animator.enabled = false;
			this.Retargeting.ForceTPose();
			this.Animator.enabled = cache;

			// instead of using "GLTF", we override based on the clip info
			var _name = KxPath.GetFileNameWithoutExtension(key.Path);
			if (Animation?.clip?.wrapMode == WrapMode.Loop)
				_name += "(loop)";
			gameObject.name = _name;
			gltf.EnableUpdateWhenOffscreen();
		}

		#region Self Despawn
		private ISpawner m_Pool = null;
		public void OnSpawn(ISpawner pool)
		{
			this.m_Pool = pool;
		}

		public void OnDespawn()
		{
		}

		public void SelfDespawn()
		{
			if (this.m_Pool == null)
			{
				Debug.LogWarning($"GxVRMAToken: {this.gameObject.name} is not spawned by a pool, cannot despawn.");
				gameObject.SetActive(false);
				return;
			}
			this.m_Pool?.Despawn(this.gameObject);
			this.m_Pool = null;
		}
		#endregion Self Despawn
	}
}