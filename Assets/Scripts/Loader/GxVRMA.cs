//#define SHOW_DEBUG_MESH
#define NO_BLENDING
using Kit2;
using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using UniGLTF;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
	/// <summary>
	/// GxVRMA is <see cref="IRetarget"/> & <see cref="GxRetargeting"/>
	/// just like <see cref="GxAnimationTask"/> but for VRMA (VRM Animation) files.
	/// </summary>
	public class GxVRMA : GxCharacterTask, IRetarget
    {
        private GxRetargeting m_from;
		[SerializeField, Range(0f, 1f)] private float m_Weight01 = 1f;

		private readonly RuntimeGltfInstance gltf;
		public GxRetargeting GetTarget() => m_from;
		public float GetWeight01() => m_Weight01;

#if NO_BLENDING
		private enum eState
		{
			None,
			PlayAni,
			Hold,
			Exit,
		}
#else
		private enum eState
		{
			None,
			PlayAni,
			BlendIn,
			Hold,
			BlendOut,
			Exit,
		}
#endif

		private eState state = eState.None;
#if !NO_BLENDING
		private BlendWeight m_BlendIn, m_BlendOut;
		public BlendWeight BlendIn => m_BlendIn;
		public BlendWeight BlendOut => m_BlendOut;
#endif
		private readonly GxCharacter character;
		private KeyValuePair<ISpawner, GameObject> m_Spawner;
		private Animation m_Animation;
		private float m_StartTime = 0f;
		public bool IsRealtime { get; private set; } = false;
		public GxVRMA(GxCharacter character, RuntimeGltfInstance gltf, float blendTime, bool isRealTime,
			ISpawner spawner, GameObject token) : base(character)
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
#if !NO_BLENDING
			this.m_BlendIn = new BlendWeight(0f, 1f, blendTime, IsRealtime);
			this.m_BlendOut = null; // Reset blend out to null initially
#endif
			Debug.Assert(spawner != null, "Spawner cannot be null.");
			Debug.Assert(token != null, "Token cannot be null.");
			this.m_Spawner = new KeyValuePair<ISpawner, GameObject>(spawner, token);
		}

		protected override bool InternalExecute()
		{
			switch (state)
			{
				case eState.None:
				PlayAnimationOnLoad();
					return true;
				case eState.PlayAni:
					++state;
					break;
#if !NO_BLENDING
				case eState.BlendIn:
					if (!m_BlendIn.Execute()) ++state;
					break;
#endif
				case eState.Hold:
					// Hold state, do nothing
					CheckPlayTime();
					break;
#if !NO_BLENDING
				case eState.BlendOut:
					if (!m_BlendOut.Execute()) ++state;
					break;
#endif
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
#if !NO_BLENDING
			m_BlendIn.Reset();
			m_BlendOut?.Reset();
			m_BlendOut = null;
#endif
		}

		private void PlayAnimationOnLoad()
		{
			gltf.EnableUpdateWhenOffscreen();

			//var animator = gltf.GetComponentInChildren<Animator>();
			//Debug.Assert(animator != null, "Animator component not found in the VRMA instance.");
			m_from = gltf.GetComponentInChildren<GxRetargeting>(true);
			//if (m_from == null)
			//{
			//	// only apply on first time (object pooling)
			//	m_from = animator.gameObject.AddComponent<GxRetargeting>();
			//	m_from.ForceTPose();
			//}

			character.AddAnimationRetarget(this);
			character.BoardcastWillPlayAnimation(this);

			m_Animation = gltf.GetComponent<Animation>();
			Debug.Assert(m_Animation != null, "Animation component not found in the VRMA instance.");
			m_Animation.cullingType = AnimationCullingType.AlwaysAnimate;
			m_Animation.Play();
			m_StartTime = Time.timeSinceLevelLoad;
			state = eState.PlayAni;

#if SHOW_DEBUG_MESH
			gltf.ShowMeshes();
			var vrma = gltf.GetComponent<Vrm10AnimationInstance>();
			if (vrma)
			{
				vrma .ShowBoxMan(true);
			}
#else
			var vrma = gltf.GetComponent<Vrm10AnimationInstance>();
			if (vrma)
			{
				vrma.ShowBoxMan(false);
			}
#endif
		}

		private void CheckPlayTime()
		{
			if (state != eState.Hold)
				return; // Not in hold state, do nothing.

			var total = m_Animation.clip.length;
			var elapsed = Time.timeSinceLevelLoad - m_StartTime;
			if (elapsed < total)
				return; // Still playing, do nothing.

#if NO_BLENDING
			state = eState.Exit; // Time is up, exit state.
#else
			TryTriggerBlendOut();
#endif
		}

		public void OnWillPlayAnimation(IRetarget other)
		{
			if (isDisposed)
				return;
#if !NO_BLENDING
			if (state >= eState.BlendOut)
				return; // Already blending out, ignore.
			if (m_BlendOut != null)
				return;

			Debug.Assert(this != other, "Cannot blend out itself, this should not happen.");

			//Debug.Log($"Attempt to blend out {m_Timeline.gameObject.name}");
			var w = this.m_BlendIn.weight;
			m_BlendOut = new BlendWeight(w, 0f, 0.25f, IsRealtime);
			TryTriggerBlendOut();
#else
			state = eState.Exit;
#endif
		}

#if !NO_BLENDING
		private void TryTriggerBlendOut()
		{
			if (state < eState.BlendIn &&
				state >= eState.BlendOut)
				return;
			if (m_BlendOut == null)
				return;
			state = eState.BlendOut;
		}
#endif

		protected override void OnDisposing()
		{
			base.OnDisposing();
			if (character != null)
			{
				character.RemoveAnimationRetarget(this);
			}
			Despawn();
		}
		private void Despawn()
		{
			if (m_Spawner.Key == null || m_Spawner.Value == null)
				return; // No valid spawner or token to despawn.
			m_Animation.Stop(); // Stop the animation before despawning.
			m_Spawner.Key.Despawn(m_Spawner.Value);
			m_Spawner = default; // Clear the spawner reference.
								 // gltf.Dispose(); // Dispose the gltf instance.
		}
	}
}