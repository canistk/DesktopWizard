using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Play Character Animation")]
	[TaskDescription("Play Character Animation.")]
	public class PlayCharacterAnimation : CharacterAction
	{
		[SerializeField] SharedGxMotionKey m_MotionKey;
		[SerializeField] SharedFloat m_FadeTime = 0.25f; // Default fade time for crossfade

		private float m_StartTime = 0f;
		private bool m_Initialized = false;
		private GxMotionTask m_MotionTask = null;
		private const float TIMEOUT = 5f; // Timeout for waiting for pose to be set

		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwCamera == null)
				return eState.Failure;
			if (Character == null)
			{
				Debug.LogError("Character is null, cannot play animation.");
				return eState.Failure;
			}

			if (!m_Initialized)
			{
				m_Initialized = true;
				m_StartTime = Time.timeSinceLevelLoad;
				try
				{
					Character.CrossFade(m_MotionKey.Value, m_FadeTime.Value,
					(task) => { 
						m_MotionTask = task;
					});
				}
				catch (System.Exception ex)
				{
					Debug.LogError($"Failed to play animation: {ex.Message}");
					return eState.Failure;
				}
			}

			if (m_Initialized && m_MotionTask == null &&
				Time.timeSinceLevelLoad - m_StartTime > TIMEOUT)
			{
				Debug.LogError($"Animation {m_MotionKey.Value.ShortName} has been playing for more than 5 seconds, check if it is looping correctly.");
				return eState.Failure; // Timeout after 5 seconds
			}

			if (m_Initialized &&
				m_MotionTask != null)
				// m_MotionTask.isCompleted)
			{
				if (m_MotionTask.isCompleted)
					return eState.Success;
				if (!m_MotionTask.IsLoop() && m_MotionTask.IsPlayedOnce())
					return eState.Success;
			}

			return eState.Running;
		}

		private void InternalReset()
		{
			m_Initialized = false;
			m_MotionTask = null;
			m_StartTime = 0f;
		}

		public override void OnReset()
		{
			base.OnReset();
			InternalReset();
		}

		public override void OnBehaviorRestart()
		{
			base.OnBehaviorRestart();
			InternalReset();
		}
	}
}
