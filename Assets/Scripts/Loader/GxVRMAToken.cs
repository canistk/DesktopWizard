using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using UniVRM10;
using UniGLTF;
namespace Gaia
{
	/// <summary>
	/// A helper class for managing Gaia VRMA tokens in Unity.
	/// </summary>
	public class GxVRMAToken : MonoBehaviour
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


		public void Setup(string clipName)
		{
			gameObject.name = clipName;

			this.Animation.cullingType = AnimationCullingType.AlwaysAnimate;
			this.Vrm10AnimationInstance.ShowBoxMan(false);

			// Enfore T-Pose for the VRMA
			var cache = this.Animator.enabled;
			this.Animator.enabled = false;
			this.Retargeting.ForceTPose();
			this.Animator.enabled = cache;

			gltf.EnableUpdateWhenOffscreen();
		}
	}
}