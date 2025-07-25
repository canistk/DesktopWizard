using Kit2.ObjectPool;
using Kit2.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxVRMAHandler : GxMotionHandler, IDisposable
    {
        public GxVRMAToken vrma;
        public float fadeIn;
		public GxVRMAHandler(GxMotionKey key, GxVRMAToken vrma, float fadeIn) : base(key)
        {
            this.vrma = vrma;
        }

		public override GxRetargeting GetRetargeting() => vrma?.Retargeting;

		public override void OnInitialize()
		{
			// vrma.gltf.ShowMeshes();
			vrma.Vrm10AnimationInstance.ShowBoxMan(false);
			vrma.Animation.Play();
		}

		public override void Update()
		{
		}

		public override void OnWillPlayAnimation(IRetarget other)
		{
			if (task.state < GxMotionTask.eState.BlendingOut)
				task?.FadeOut(fadeOutDuration: 0f, realTime: true);
		}

		public override void SelfDespawn()
		{
			vrma.Animation.Stop();
			vrma.SelfDespawn();
		}

		void IDisposable.Dispose()
		{
			vrma?.SelfDespawn();
			vrma = null;
		}
	}

    public class GxTimelineHandler : GxMotionHandler, IDisposable
    {
        public GxTimelineAsset timeline;
        public float fadeIn;
        public GxTimelineHandler(GxMotionKey key, GxTimelineAsset timeline, float fadeIn) : base(key)
        {
            this.timeline = timeline;
            this.fadeIn = fadeIn;
        }
		public override GxRetargeting GetRetargeting() => timeline?.GetRetargeting();

		public override void OnInitialize()
		{
			//TODO:
			// timeline.playableDirector.Play();
			var pd = timeline?.playableDirector;
			if (pd == null)
			{
				Debug.LogError("PlayableDirector is null, cannot initialize timeline handler.");
				return;
			}
			timeline.EVENT_PlayedOneCycle += Timeline_EVENT_PlayedOneCycle;
			if (!timeline.playableDirector.playOnAwake)
				timeline.playableDirector.Play();
		}

		public override void Update() { }

		public override void OnWillPlayAnimation(IRetarget other)
		{
			if (task.state < GxMotionTask.eState.BlendingOut)
				task?.FadeOut(fadeOutDuration: 0f, realTime: true);
		}

		public override void SelfDespawn()
		{
			timeline.EVENT_PlayedOneCycle -= Timeline_EVENT_PlayedOneCycle;
			timeline.SelfDespawn();
		}

		private void Timeline_EVENT_PlayedOneCycle()
		{
			// motionData.ClipLength
			timeline.EVENT_PlayedOneCycle -= Timeline_EVENT_PlayedOneCycle;
			Character?.BoardCastPlayedOnce(task);
			// TODO: play next animation if exists
			// motionData.Next
		}

		void IDisposable.Dispose()
		{
			if (timeline != null)
				timeline.EVENT_PlayedOneCycle -= Timeline_EVENT_PlayedOneCycle;
			timeline = null;
		}
	}
	
    public abstract class GxMotionHandler : ISelfDespawnable
    {
        public readonly GxMotionKey key;
		public readonly GxMotionData motionData;
		public GxCharacter Character => task?.Character;
		public GxMotionTask task { get; private set; } = null;

		public GxMotionHandler(GxMotionKey key)
        {
            this.key = key;
			if (!GxMotionDatabase.TryGetMotion(key, out motionData))
			{
				Debug.LogError($"Motion data not found for key: {key}");
			}
		}
		internal void SetTask(GxMotionTask task)
		{
			this.task = task;
		}


		public abstract GxRetargeting GetRetargeting();

		public abstract void OnInitialize();

		public abstract void Update();

		public abstract void OnWillPlayAnimation(IRetarget other);

		public abstract void SelfDespawn();
	}

    public class GxMotionTask : GxCharacterAnimationTask, IRetarget
    {
        public override GxRetargeting GetTarget() => m_Handler?.GetRetargeting();
        public override float GetWeight01() => 1f; // Default weight for motion tasks

		private BlendWeight m_BlendIn, m_BlendOut;
        private GxMotionHandler m_Handler;

		public enum eState
		{
			None = 0,
			Initializing = 1, // Boardcast signal to other MotionTasks to prepare
			BlendingIn = 2, // Blending in the motion
			Playing = 3, // Playing the motion, or looping it
			BlendingOut = 4, // Blending out the motion
			Completed = 5, // Exiting the motion
			Error = 10,
		}
		public eState state { get; private set; } = eState.None;

		public GxMotionTask(GxCharacter character, GxMotionHandler handler, float fadeIn, bool realTime = false)
			: this(character, handler, new BlendWeight(0f, 1f, fadeIn, realTime))
		{ }

		public GxMotionTask(GxCharacter character, GxMotionHandler handler, BlendWeight fadeIn) : base(character)
        {
			state = eState.Initializing;
			if (character == null)
            {
                Debug.LogError("GxMotionTask requires a valid GxCharacter reference.");
				state = eState.Error;
				return;
            }
            if (handler == null)
            {
                Debug.LogError("GxMotionTask requires a valid GxMotionHandler reference.");
				state = eState.Error;
				return;
            }
			handler.SetTask(this);
			this.m_Handler = handler;
            this.m_BlendIn = fadeIn;
            this.m_BlendOut = null; // Reset blend out to null initially
        }

		protected override bool InternalExecute()
		{
			if (isDisposed || isCompleted)
				return false; // end task
			try
			{
				switch (state)
				{
					case eState.None: throw new System.Exception("Logic error: GxMotionTask should not be in None state after constructor.");
					case eState.Initializing: Initialize(); break;
					case eState.BlendingIn:
					{
						var running = m_BlendIn.Execute();
						if (!running)
							++state; // Transition to Playing state after blending in
					}
					break;
					case eState.Playing: break;
					case eState.BlendingOut:
					{
						var running = m_BlendOut.Execute();
						if (!running)
							++state; // Transition to Completed state after blending out
					}
					break;
					case eState.Completed: return false; // Task is complete, no update.
					default: throw new System.NotImplementedException($"State {state} not implemented in GxMotionTask.");
				}

				if (state < eState.Completed)
				{
					m_Handler.Update();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"MotionTask Error {state}: {ex.Message}");
				state = eState.Error;
				return false; // Stop execution on error
			}

			return state < eState.Completed;
		}

		private void Initialize()
		{
			if (state != eState.Initializing)
				throw new System.Exception($"Logic error: GxMotionTask should never in {state} state when initializing.");
			Character.AddAnimationRetarget(this);
			Character.BoardcastWillPlayAnimation(this);
			m_Handler.OnInitialize();
			// Transition to blending in or playing directly
			state = m_BlendIn == null ? eState.Playing : eState.BlendingIn;
		}

		protected override void OnDisposing()
		{
			Character?.RemoveAnimationRetarget(this);
			m_Handler?.SelfDespawn();
			base.OnDisposing();
			m_Handler = null; // Clear the handler reference
			m_BlendIn = null;
			m_BlendOut = null;
		}

		public override void OnWillPlayAnimation(IRetarget other)
		{
			// TODO: Handle when another animation is about to play
			try
			{
				m_Handler.OnWillPlayAnimation(other);
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Error in OnWillPlayAnimation: {ex.Message}");
				state = eState.Error; // Set to error state on exception
			}
		}

		public void FadeOut(float fadeOutDuration, bool realTime = false)
		{
			if (isDisposed || isCompleted)
				return;
			var w = GetWeight01();
			var blend = new BlendWeight(w, 0f, fadeOutDuration, realTime);
			FadeOut(blend);
		}

		public void FadeOut(BlendWeight blend)
		{
			if (isDisposed || isCompleted)
				return;
			if (state >= eState.BlendingOut)
				return; // Already blending out, do nothing
			if (m_BlendOut != null)
			{
				Debug.LogWarning("GxMotionTask already has a blend out task, ignoring new request.");
				return; // Already has a blend out task, do nothing
			}
			this.m_BlendOut = blend;
			state = eState.BlendingOut;
		}

		public void Stop()
		{
			if (isDisposed || isCompleted)
				return; // Already disposed or completed, do nothing	
			if (state >= eState.Completed)
				return; // Error state, do nothing
			state = eState.Completed;
		}
	}

	/// <summary>
	/// Handling the blending of weights over a specified duration.
	/// </summary>
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

		public bool IsComplete()
		{
			if (duration <= float.Epsilon)
				return true;
			if (!m_StartTime.Key)
				return false; // Not started yet
			var time = GetTime();
			return time - m_StartTime.Value >= duration;
		}

		public override bool Execute()
		{
			if (duration <= float.Epsilon)
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

}