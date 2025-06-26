using Kit2.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace Gaia
{
	public class GxAnimationTask : GxCharacterTask, IRetarget
	{
		GxTimelineAsset m_Timeline;
		GxRetargeting fromBiped => m_Timeline?.GetRetargeting();
		GxCharacter m_Character;

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
		
		public bool IsRealtime { get; private set; } = false;
		public GxAnimationTask(GxCharacter character, GxTimelineAsset timeline, float blendTime, bool isRealTime) : base(character)
		{
			if (character == null)
			{
				Debug.LogError("GxAnimationTask requires a valid GxCharacter reference.");
				return;
			}
			if (timeline == null)
			{
				Debug.LogError($"The {nameof(GxRetargeting)} reference NOT found, cannot bind to character.");
				return;
			}
			this.m_Timeline = timeline;
			this.m_Character = character;
			this.IsRealtime = isRealTime;
			this.m_BlendIn = new BlendWeight(0f, 1f, blendTime, IsRealtime);
			this.m_BlendOut = null; // Reset blend out to null initially
		}

		private void PlayTimelineOnloaded()
		{
			var pd = m_Timeline.playableDirector;

			// Hook up the retargeting system
			m_Character.Retargeting.AddTarget(this);
			m_Character.BoardcastWillPlayAnimation(this);

			m_Timeline.EVENT_PlayedOneCycle += OnPlayedOneCycle;
			if (!pd.playOnAwake)
				pd.Play();
			state = eState.PlayAni;
		}

		protected override bool InternalExecute()
		{
			switch (state)
			{
				case eState.None:
					PlayTimelineOnloaded();
					break;
				case eState.PlayAni:
					++state;
					break;
				case eState.BlendIn:
					if (!m_BlendIn.Execute()) ++state;
					break;
				case eState.Hold:
					// Just wait for next animation.
				break;
				case eState.BlendOut:
					if (!m_BlendOut.Execute()) ++state;
					break;
				case eState.Exit:
					break;
				default:
					throw new System.NotImplementedException();
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
			m_Timeline.Despawn();
			m_Character.Retargeting.RemoveTarget(this);
		}

		private void OnPlayedOneCycle()
		{
			// Debug.Log($"{m_Timeline.gameObject.name} played Once");
			m_Timeline.EVENT_PlayedOneCycle -= OnPlayedOneCycle;
			m_Character.BoardCastPlayedOnce(this);
			
			if (!m_Timeline.IsLoop && m_BlendOut == null)
			{
				m_BlendOut = new BlendWeight(m_BlendIn.weight, 0f, m_BlendIn.duration, IsRealtime);
				TryTriggerBlendOut();
			}
			/// else,
			/// If it's looping, we just hold the blend in weight until next animation.
			/// <see cref="OnWillPlayAnimation(GxAnimationTask)"/>
		}

		public void OnWillPlayAnimation(GxAnimationTask other)
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
			var duration = other.m_BlendIn.duration;
			m_BlendOut = new BlendWeight(w, 0f, duration, other.IsRealtime);
			TryTriggerBlendOut();
		}

		/// <summary>
		/// Played one cycle, <see cref="OnPlayedOneCycle"/>
		/// Blend out by others <see cref="OnWillPlayAnimation(GxAnimationTask)"/>
		/// 
		/// </summary>
		private void TryTriggerBlendOut()
		{
			if (state < eState.BlendIn &&
				state >= eState.BlendOut)
				return;
			if (m_BlendOut == null)
				return;
			state = eState.BlendOut;
		}

		public float GetWeight01()
		{
			if (isDisposed)
				return 0f;

			// assume that the blend in is always before blend out.
			return state < eState.BlendOut ?
				m_BlendIn.weight :
				m_BlendOut.weight;
		}

		public GxRetargeting GetTarget()
		{
			return fromBiped;
		}
	}

	public class BlendWeight : MyTaskBase
	{
		private readonly float start, end;
		private readonly bool realTime;
		public float weight { get; private set; } = 0f;
		public float duration { get; private set; } = 0f;
		public BlendWeight(float startWeight01, float targetWeight01, float duration, bool realTime)
		{
			this.start = Mathf.Clamp01(startWeight01);
			this.end = Mathf.Clamp01(targetWeight01);
			this.duration = Mathf.Max(0f, duration);
			this.realTime = realTime;
			this.weight = start; // Initialize weight to start value
		}

		protected KeyValuePair<bool, float> m_StartTime;

		private float GetTime()
		{
			return realTime ? Time.realtimeSinceStartup : Time.time;
		}

		public override bool Execute()
		{
			if (duration <= 0f)
			{
				weight = end; // Instant transition
				return false; // Task is complete
			}

			var time = GetTime();

			if (!m_StartTime.Key)
			{
				m_StartTime = new KeyValuePair<bool, float>(true, time);
				weight = start;
			}

			float elapsed = time - m_StartTime.Value;
			if (elapsed >= duration)
			{
				weight = end;
				return false; // Task is complete
			}

			// Interpolate the weight based on elapsed time
			float pt = elapsed / duration;
			weight = Mathf.Lerp(start, end, pt);
			return true;
		}

		public override void Reset()
		{
			base.Reset();
			m_StartTime = default;
			weight = start;
		}
	}

	/// <summary>
	/// To download and load an asset from Addressables.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class GxAddressableTask<T> : MyTaskWithState
	{
 		private readonly string m_AssetPath;
		private readonly AssetReference m_AssetReference;
		private readonly bool m_ByRef;
		private AsyncOperationHandle<T> m_Oper;
		public AsyncOperationHandle<T> Operation => m_Oper;
		private System.Action<T> m_Success;
		private System.Action<System.Exception> m_Fail;
		public GxAddressableTask(string assetPath,
			System.Action<T> success,
			System.Action<System.Exception> fail)
		{
			this.m_AssetPath = assetPath;
			this.m_AssetReference = default;
			this.m_ByRef = false;
			this.m_Success = success;
			this.m_Fail = fail;
		}
		public GxAddressableTask(AssetReference assetReference,
			System.Action<T> success,
			System.Action<System.Exception> fail)
		{
			this.m_AssetPath = string.Empty;
			this.m_AssetReference = assetReference;
			this.m_ByRef = true;
			this.m_Success = success;
			this.m_Fail = fail;
		}

		protected override void OnEnter()
		{
			m_Oper = m_ByRef ?
				m_AssetReference.LoadAssetAsync<T>() :
				Addressables.LoadAssetAsync<T>(m_AssetPath);
		}

		protected override bool ContinueOnNextCycle()
		{
			// Continue if the asset is still loading
			if (m_Oper.Status == AsyncOperationStatus.Failed)
				return false;

			return !m_Oper.IsDone;
		}

		protected override void OnComplete()
		{
			//Debug.Log($"Successfully loaded asset: {m_Oper.DebugName}");
			try
			{
				if (m_Oper.Status == AsyncOperationStatus.Failed)
				{
					throw m_Oper.OperationException;
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error during asset loading: {ex.Message}");
				m_Fail?.Invoke(ex);
				return;
			}

			m_Success?.Invoke(m_Oper.Result);
		}

		public override void Reset()
		{
			base.Reset();
			// TODO: if it's downloaded, keep the handle for next time.
			if (m_Oper.IsValid())
			{
				Addressables.Release(m_Oper);
				m_Oper = default;
			}
		}
	}
}