using Kit2;
using System.Collections;
using System.Collections.Generic;
using UniGLTF;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UniVRM10;
namespace Gaia
{
	/// <summary>
	/// GxVRMA is <see cref="IRetarget"/> & <see cref="GxRetargeting"/>
	/// just like <see cref="GxAnimationTask"/> but for VRMA (VRM Animation) files.
	/// </summary>
	public class GxVRMA : GxCharacterTask, IRetarget
    {
        private GxRetargeting from;
		[SerializeField, Range(0f, 1f)] private float m_Weight01 = 1f;

		private readonly RuntimeGltfInstance gltf;
		public GxRetargeting GetTarget() => from;
		public float GetWeight01() => m_Weight01;

		private enum eState
		{
			None,
			PlayAni,
			BlendIn,
			Hold,
			BlendOut,
			Exit,
		}
		private eState state = eState.None;
		private BlendWeight m_BlendIn, m_BlendOut;
		private readonly GxCharacter character;
		private Vrm10AnimationInstance m_Vrma;
		public bool IsRealtime { get; private set; } = false;
		public GxVRMA(GxCharacter character, RuntimeGltfInstance gltf, float blendTime, bool isRealTime) : base(character)
		{
			if (character == null)
			{
				Debug.LogError("GxVRMA requires a valid GxCharacter reference.");
				return;
			}
			if (gltf == null)
			{
				Debug.LogError($"The {nameof(GxRetargeting)} reference NOT found, cannot bind to character.");
				return;
			}
			this.gltf = gltf;
			this.character = character;
			this.IsRealtime = isRealTime;
			this.m_BlendIn = new BlendWeight(0f, 1f, blendTime, IsRealtime);
			this.m_BlendOut = null; // Reset blend out to null initially
		}

		protected override bool InternalExecute()
		{
			switch (state)
			{
				case eState.None:
				PlayAnimationOnLoad();
				state = eState.PlayAni;
					return true;
				case eState.PlayAni:
					++state;
					break;
				case eState.BlendIn:
					if (!m_BlendIn.Execute()) ++state;
					break;
				case eState.Hold:
					// Hold state, do nothing
					break;
				case eState.BlendOut:
					if (!m_BlendOut.Execute()) ++state;
					break;
				case eState.Exit:
					break;
				default:
					throw new System.NotImplementedException($"State {state} not implemented in GxVRMA.");
			}
			if (state == eState.Exit)
			{
				Abort();
			}
			return state < eState.Exit;
		}

		public override void Reset()
		{
			base.Reset();
			state = eState.None;
			m_BlendIn.Reset();
			m_BlendOut?.Reset();
			m_BlendOut = null;
		}

		protected override void OnDisposing()
		{
			base.OnDisposing();
			if (character != null)
			{
				character.RemoveAnimationRetarget(this);
			}
		}

		private void PlayAnimationOnLoad()
		{
			gltf.EnableUpdateWhenOffscreen();
#if DEBUG
			gltf.ShowMeshes();
#endif
			m_Vrma = gltf.GetComponent<Vrm10AnimationInstance>();
			if (m_Vrma)
			{
#if DEBUG
				m_Vrma.ShowBoxMan(true);
#endif
			}
			var animator = gltf.GetComponentInChildren<Animator>();
			if (animator)
			{
				var retargeting = animator.GetComponent<GxRetargeting>();
				if (retargeting == null)
				{
					retargeting = animator.gameObject.AddComponent<GxRetargeting>();
					retargeting.ForceTPose();
				}
			}
			character.AddAnimationRetarget(this);
			var animation = gltf.GetComponent<Animation>();
			if (animation)
			{
				animation.Play();
			}
		}


	}
}