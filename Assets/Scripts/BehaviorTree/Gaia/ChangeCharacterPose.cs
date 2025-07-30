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
		private enum eMethod
		{
			SpecifyPose,
			RandomPose,
			RandomPoseByTags,
		}
		[SerializeField] SharedFloat m_FadeTime = 0.25f; // Default fade time for cross fade
		[SerializeField] eMethod m_Method = eMethod.SpecifyPose;

		[Header("Specify the pose to change to")]
		[SerializeField] SharedString m_PoseKey;

		[Header("Random Pose Settings (space to split)")]
		[SerializeField] SharedString m_IncludeTags;
		[SerializeField] SharedString m_ExcludeTags;
		[SerializeField] SharedInt m_MinCount = 1;

		private bool m_Initialized = false;
		private GxPoseTask m_PoseTask = null;
		private const float TIMEOUT = 5f; // Timeout for waiting for pose to be set
		private float m_StartTime = 0f;

		private enum eStyle
		{
			EnterPoseAsSuccess,
			SuccessOnPoseExit,
			FailOnPoseExit,
		}

		[Header("Exit Method")]
		[SerializeField] eStyle m_Style = eStyle.EnterPoseAsSuccess;

		private static readonly char[] SPLITS = new char[] { ',', ' ', '-' };

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

					switch (m_Method)
					{
						case eMethod.SpecifyPose:
						{
							if (string.IsNullOrEmpty(m_PoseKey.Value))
							{
								Debug.LogError("Pose key is empty, cannot change pose.");
								return eState.Failure;
							}

							Character.ChangePose(m_PoseKey.Value, fadeIn,
							(task) =>
							{
								m_PoseTask = task;
							});
						}
						break;
						case eMethod.RandomPose:
						// Random pose will be handled later
						if (GxMotionDatabase.TryGetRandomPoseKey(out var pose))
						{
							var poseKey = pose.key;
							Character.ChangePose(poseKey, fadeIn,
							(task) =>
							{
								m_PoseTask = task;
							});
						}
						break;
						case eMethod.RandomPoseByTags:
						{
							var includeTags = m_IncludeTags.IsNone || string.IsNullOrEmpty(m_IncludeTags.Value) ? new string[0] : m_IncludeTags.Value.Split(SPLITS);
							var excludeTags = m_ExcludeTags.IsNone || string.IsNullOrEmpty(m_ExcludeTags.Value) ? new string[0] : m_ExcludeTags.Value.Split(SPLITS);
							var minCount = m_MinCount.IsNone ? 1 : m_MinCount.Value;
							if (GxMotionDatabase.TryGetRandomPoseByTags(includeTags, excludeTags, minCount, out var poseByTags))
							{
								var poseKey = poseByTags.key;
								Character.ChangePose(poseKey, fadeIn,
								(task) =>
								{
									m_PoseTask = task;
								});
							}
							else
							{
								Debug.LogError($"No pose found for tags, include: {m_IncludeTags?.Value}, exclude: {m_ExcludeTags?.Value}");
								return eState.Failure;
							}
						}
						break;
						default:
							Debug.LogError($"Unknown method: {m_Method}");
							return eState.Failure;
					}
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

			if (m_Style == eStyle.EnterPoseAsSuccess &&
				m_Initialized &&
				m_PoseTask != null)
			{
				// If the pose task is still running, we return Running state
				return eState.Success; // Pose set successfully
			}
			

			if (m_Initialized &&
				m_PoseTask != null &&
				m_PoseTask.isCompleted)
			{
				// If the pose task is still running, we return Running state
				if (m_Style == eStyle.FailOnPoseExit)
				{
					return eState.Failure; // Pose failed to set
				}
				return eState.Success; // Pose set successfully
			}
			return eState.Running;
		}

		private void InternalReset()
		{
			m_Initialized = false;
			m_PoseTask = null;
			m_StartTime = 0f;
		}

		public override void OnEnd()
		{
			base.OnEnd();
			InternalReset();
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