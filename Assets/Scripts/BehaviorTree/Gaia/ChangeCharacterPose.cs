using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Change Character Pose")]
	[TaskDescription("Change Character Pose.")]
	public class ChangeCharacterPose : CharacterAction
	{
		[SerializeField] SharedBool m_RandomPose = false;
		[SerializeField] SharedString m_PoseKey;
		[SerializeField] SharedFloat m_FadeTime = 0.25f; // Default fade time for crossfade

		private bool m_Initialized = false;
		private GxPoseTask m_PoseTask = null;
		private const float TIMEOUT = 5f; // Timeout for waiting for pose to be set
		private float m_StartTime = 0f;

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
				try
				{
					if (m_PoseKey.IsNone)
						return eState.Failure; // No pose key specified
					var fadeIn = m_FadeTime.IsNone ? 0f : m_FadeTime.Value;
					m_StartTime = Time.timeSinceLevelLoad;

					var poseKey = m_PoseKey.Value;

					if (!m_RandomPose.IsNone && m_RandomPose.Value)
					{
						// override by Randomize pose key when m_RandomPose is true
						if (GxMotionDatabase.TryGetRandomPoseKey(out var pose))
						{
							poseKey = pose.key;
						}
					}

					Character.ChangePose(poseKey, fadeIn,
					(task) => { 
						m_PoseTask = task;
					});
				}
				catch (System.Exception ex)
				{
					Debug.LogError($"Failed to set pose: {ex.Message}");
					return eState.Failure;
				}
			}

			if (m_Initialized && m_PoseTask == null &&
				Time.timeSinceLevelLoad - m_StartTime > TIMEOUT)
			{
				Debug.LogError($"Pose {m_PoseKey.Value} has been setting for more than {TIMEOUT} seconds, check if it is looping correctly.");
				return eState.Failure; // Timeout after 5 seconds
			}

			if (m_Initialized &&
				m_PoseTask != null &&
				m_PoseTask.isCompleted)
			{
				// If the pose task is still running, we return Running state
				return eState.Success; // Pose set successfully
			}
			return eState.Running;
		}

		public override void OnReset()
		{
			base.OnReset();
			m_Initialized = false;
			m_PoseKey = default;
		}

		public override void OnBehaviorRestart()
		{
			base.OnBehaviorRestart();
			m_Initialized = false;
			m_PoseKey = default;
		}
	}
}