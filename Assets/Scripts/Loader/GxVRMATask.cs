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
	/// just like <see cref="GxTimelineTask"/> but for VRMA (VRM Animation) files.
	/// </summary>
	public class GxVRMATask : GxCharacterTask, IRetarget
    {
		[SerializeField, Range(0f, 1f)] private float m_Weight01 = 1f;

		private readonly RuntimeGltfInstance gltf;
		public GxRetargeting GetTarget() => m_VRMA.Retargeting;
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
		private GxVRMAToken m_VRMA;
		private float m_StartTime = 0f;
		public bool IsRealtime { get; private set; } = false;
		public GxVRMATask(GxCharacter character, RuntimeGltfInstance gltf, float blendTime, bool isRealTime,
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
			/// Assume prefab is already loaded and contains a GxVRMAToken component.
			/// <see cref="GxCharacter.SetupVRMAPrefab(GameObject, string)"/>
			m_VRMA = gltf.GetComponentInChildren<GxVRMAToken>(true);
			
			character.AddAnimationRetarget(this);
			character.BoardcastWillPlayAnimation(this);

			m_VRMA.Animation.Play();
			m_StartTime = Time.timeSinceLevelLoad;
			state = eState.PlayAni;

#if SHOW_DEBUG_MESH
			gltf.ShowMeshes();
			m_VRMA.Vrm10AnimationInstance.ShowBoxMan(true);
#else
			m_VRMA.Vrm10AnimationInstance.ShowBoxMan(false);
#endif
		}

		private void CheckPlayTime()
		{
			if (state != eState.Hold)
				return; // Not in hold state, do nothing.
			if (m_VRMA.Animation.clip.wrapMode == WrapMode.Loop)
				return; // Looping animation, do nothing.
			var total = m_VRMA.Animation.clip.length;
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
			m_VRMA.Animation.Stop(); // Stop the animation before despawning.
			m_Spawner.Key.Despawn(m_Spawner.Value);
			m_Spawner = default; // Clear the spawner reference.
			// gltf.Dispose(); // Dispose the gltf instance.
		}
	}
}