using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxMotionTask : GxCharacterAnimationTask, IRetarget
    {
        public override GxRetargeting GetTarget() => m_Handler?.GetRetargeting();
        public override float GetWeight01() => 1f; // Default weight for motion tasks

		private BlendWeight m_BlendIn, m_BlendOut;
        private GxMotionHandler m_Handler;
		private float m_StartTime = 0f;
		public enum eMState
		{
			None = 0,
			Initializing, // Boardcast signal to other MotionTasks to prepare
			BlendingIn, // Blending in the motion
			Playing, // Playing the motion, or looping it
			BlendingOut, // Blending out the motion
			Completed, // Exiting the motion
			Error = 10,
			Disposed = 11,
		}
		protected eMState m_State { get; private set; } = eMState.None;

		public GxMotionKey Key => m_Handler?.key ?? GxMotionKey.Invalid;

		public GxMotionHandler Handler => m_Handler;

		public GxMotionTask(GxCharacter character, GxMotionHandler handler, float fadeIn)
			: this(character, handler, new BlendWeight(0f, 1f, fadeIn, (character?.animator.updateMode == AnimatorUpdateMode.UnscaledTime)))
		{ }

		private GxMotionTask(GxCharacter character, GxMotionHandler handler, BlendWeight fadeIn) : base(character)
        {
			m_State = eMState.Initializing;
			if (character == null)
            {
                Debug.LogError("GxMotionTask requires a valid GxCharacter reference.");
				m_State = eMState.Error;
				return;
            }
            if (handler == null)
            {
                Debug.LogError("GxMotionTask requires a valid GxMotionHandler reference.");
				m_State = eMState.Error;
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
				switch (m_State)
				{
					case eMState.None: throw new System.Exception("Logic error: GxMotionTask should not be in None mState after constructor.");
					case eMState.Initializing: Initialize(); break;
					case eMState.BlendingIn:
					{
						var running = m_BlendIn.Execute();
						if (!running)
							m_State = eMState.Playing; // Transition to Playing mState after blending in
					}
					break;
					case eMState.Playing: break;
					case eMState.BlendingOut:
					{
						var running = m_BlendOut.Execute();
						if (!running)
						{
							m_State = eMState.Completed; // Transition to Completed mState after blending out
							Debug.Log("Blend out completed");
						}
					}
					break;
					case eMState.Completed: return false; // Task is complete, no update.
					default: throw new System.NotImplementedException($"mState {m_State} not implemented in GxMotionTask.");
				}

				if (m_State < eMState.Completed)
				{
					m_Handler.Update();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"MotionTask Error {m_State}: {ex.Message}");
				m_State = eMState.Error;
				return false; // Stop execution on error
			}

			return m_State < eMState.Completed;
		}

		private void Initialize()
		{
			if (m_State != eMState.Initializing)
				throw new System.Exception($"Logic error: GxMotionTask should never in {m_State} mState when initializing.");
			Debug.Log($"Initialize GxMotionTask: {Key.ShortName} in mState {m_State}, Character={Character}");
			// Step 0: ready to next state.
			m_State = m_BlendIn == null ? eMState.Playing : eMState.BlendingIn;

			// Step 1: Set start time
			m_StartTime = Time.timeSinceLevelLoad;

			// Step 2: Notify character about the upcoming animation
			Character.BoardcastWillPlayAnimation(this);

			// Step 3: play the animation
			m_Handler.OnInitialize();

			// Step 4: hook into character's retargeting system
			Character.AddAnimationRetarget(this);
		}

		protected override void OnDisposing()
		{
			// Debug.Log($"Disposing GxMotionTask: {Key.ShortName} in mState {m_State}, Character={Character}");
			Character.RemoveAnimationRetarget(this);
			m_Handler.SelfDespawn();
			base.OnDisposing();
			m_Handler = null; // Clear the handler reference
			m_BlendIn = null;
			m_BlendOut = null;
			m_State = eMState.Disposed;
		}

		public override void OnWillPlayAnimation(IRetarget other)
		{
			// TODO: Handle when another animation is about to play
			if (isDisposed || isCompleted)
				return;
			if (other == this)
				return;
			try
			{
				m_Handler.OnWillPlayAnimation(other);
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Error in OnWillPlayAnimation: {ex.Message}");
				m_State = eMState.Error; // Set to error mState on exception
			}
		}

		public void FadeOut(float fadeOutDuration)
		{
			if (isDisposed || isCompleted)
				return;
			var w = GetWeight01();
			var realTime = Character?.animator?.updateMode == AnimatorUpdateMode.UnscaledTime;
			var blend = new BlendWeight(w, 0f, fadeOutDuration, realTime);
			FadeOut(blend);
		}

		public void FadeOut(BlendWeight blend)
		{
			if (isDisposed || isCompleted)
				return;
			if (m_State >= eMState.BlendingOut)
				return; // Already blending out, do nothing
			if (m_BlendOut != null)
			{
				Debug.LogWarning("GxMotionTask already has a blend out task, ignoring new request.");
				return; // Already has a blend out task, do nothing
			}
			//Debug.Log($"GxMotionTask FadeOut: {Key.ShortName} with blend: {blend}, w={GetWeight01()}");
			this.m_BlendOut = blend;
			m_State = eMState.BlendingOut;
		}

		public bool IsPlayedOnce()
		{
  			if (isDisposed || isCompleted)
				return true; // Task is not active
			if (Key.Equals(GxMotionKey.Invalid))
				return false;

			var duration = m_Handler?.motionData?.ClipLength ?? 0f;
			var playedTime = Time.timeSinceLevelLoad - m_StartTime;
			return playedTime >= duration;
		}
	}
}