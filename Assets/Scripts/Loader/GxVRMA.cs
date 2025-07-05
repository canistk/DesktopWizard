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
		public BlendWeight BlendIn => m_BlendIn;
		public BlendWeight BlendOut => m_BlendOut ??= new BlendWeight(1f, 0f, 0.5f, IsRealtime);

		private readonly GxCharacter character;
		private Vrm10AnimationInstance m_Vrma;
		private Animation m_Animation;
		private float m_StartTime = 0f;
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
					CheckPlayTime();
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
			character.BoardcastWillPlayAnimation(this);


			m_Animation = gltf.GetComponent<Animation>();
			m_Animation.cullingType = AnimationCullingType.AlwaysAnimate;
			if (m_Animation)
			{
				m_Animation.Play();
				// m_Animation.clip.length;
				m_StartTime = Time.timeSinceLevelLoad;
			}
		}

		private void CheckPlayTime()
		{
			if (state != eState.Hold)
				return; // Not in hold state, do nothing.

			var total = m_Animation.clip.length;
			var elapsed = Time.timeSinceLevelLoad - m_StartTime;
			if (elapsed < total)
				return; // Still playing, do nothing.

			TryTriggerBlendOut();
		}

		public void OnWillPlayAnimation(IRetarget other)
		{
			if (isDisposed)
				return;
			if (state >= eState.BlendOut)
				return; // Already blending out, ignore.
			if (m_BlendOut != null)
				return;

			Debug.Assert(this != other, "Cannot blend out itself, this should not happen.");

			//Debug.Log($"Attempt to blend out {m_Timeline.gameObject.name}");
			var w = this.m_BlendIn.weight;
			// var duration = other.BlendIn.duration;
			var duration = 0.25f;
			m_BlendOut = new BlendWeight(w, 0f, duration, IsRealtime);
			TryTriggerBlendOut();
		}

		private void TryTriggerBlendOut()
		{
			if (state < eState.BlendIn &&
				state >= eState.BlendOut)
				return;
			if (m_BlendOut == null)
				return;
			state = eState.BlendOut;
		}
	}
}