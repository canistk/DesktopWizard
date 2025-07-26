using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
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
		public enum eState
		{
			None = 0,
			Initializing, // Boardcast signal to other MotionTasks to prepare
			Regist,
			BlendingIn, // Blending in the motion
			Playing, // Playing the motion, or looping it
			BlendingOut, // Blending out the motion
			Completed, // Exiting the motion
			Error = 10,
		}
		public eState state { get; private set; } = eState.None;

		public GxMotionKey Key => m_Handler?.key ?? GxMotionKey.Invalid;

		public GxMotionHandler Handler => m_Handler;

		public GxMotionTask(GxCharacter character, GxMotionHandler handler, float fadeIn)
			: this(character, handler, new BlendWeight(0f, 1f, fadeIn, (character?.animator.updateMode == AnimatorUpdateMode.UnscaledTime)))
		{ }

		private GxMotionTask(GxCharacter character, GxMotionHandler handler, BlendWeight fadeIn) : base(character)
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
					case eState.Regist:
					{
						// hook into character's retargeting system
						Character.AddAnimationRetarget(this);
						state = m_BlendIn == null ? eState.Playing : eState.BlendingIn;
					}
					break;
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
			
			// Notify character about the upcoming animation
			Character.BoardcastWillPlayAnimation(this);
			
			// play the animation
			m_Handler.OnInitialize();
			m_StartTime = Time.timeSinceLevelLoad;

			// hook into character's retargeting system
			// Character.AddAnimationRetarget(this);

			// Transition to blending in or playing directly
			++state;
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
			if (isDisposed || isCompleted)
				return;
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

		public bool IsPlayedOnce()
		{
  			if (isDisposed || isCompleted)
				return true; // Task is not active
			if (state > eState.Playing)
				return true;

			var duration = m_Handler?.motionData?.ClipLength ?? 0f;
			var playedTime = Time.timeSinceLevelLoad - m_StartTime;
			return playedTime >= duration;
		}
	}
}