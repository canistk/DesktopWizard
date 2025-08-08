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
		private enum eMethod
		{
			SpecifyMotion,
			RandomMotion,
			RandomMotionByTags,
		}

		[SerializeField] SharedFloat m_FadeTime = 0.25f; // Default fade time for crossfade
		[SerializeField] eMethod m_Method = eMethod.SpecifyMotion;

		[Header("Specify the motion to change to")]
		[SerializeField] SharedGxMotionKey m_MotionKey;

		[Header("Random Motion Setting (space to split")]
		[SerializeField] SharedString m_IncludeTags;
		[SerializeField] SharedString m_ExcludeTags;
		[SerializeField] SharedInt m_MinCount = 1;

		private bool m_Initialized = false;
		private GxMotionTask m_MotionTask = null;
		private const float TIMEOUT = 5f; // Timeout for waiting for pose to be set
		private float m_StartTime = 0f;

		private enum eStyle
		{
			EnterMotionAsSuccess,
			SuccessOnMotionExit,
			PlayOnceAsSuccess,
		}

		[Header("Exit Method")]
		[SerializeField] eStyle m_Style = eStyle.EnterMotionAsSuccess;
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
					m_StartTime = Time.timeSinceLevelLoad;
					var fadeIn = m_FadeTime.IsNone ? 0f : m_FadeTime.Value;
					switch (m_Method)
					{
						case eMethod.SpecifyMotion:
						{
							if (m_MotionKey.IsNone)
								return eState.Failure;
							var key = m_MotionKey.Value;
							if (string.IsNullOrEmpty(key.Path))
							{
								Debug.LogError($"Invalid Motion key {key}");
								return eState.Failure;
							}
							Character.CrossFade(m_MotionKey.Value, fadeIn, OnMotionTaskLocated);
						}
						break;

						case eMethod.RandomMotion:
						{
							if (!GxMotionDatabase.TryGetRandomMotionKey(out var motion))
								return eState.Failure;
							Character.CrossFade(motion.Key, fadeIn, OnMotionTaskLocated);
						}
						break;

						case eMethod.RandomMotionByTags:
						{
							var includeTags = m_IncludeTags.IsNone || string.IsNullOrEmpty(m_IncludeTags.Value) ? new string[0] : m_IncludeTags.Value.Split(SPLITS);
							var excludeTags = m_ExcludeTags.IsNone || string.IsNullOrEmpty(m_ExcludeTags.Value) ? new string[0] : m_ExcludeTags.Value.Split(SPLITS);
							var minCount = m_MinCount.IsNone ? 1 : m_MinCount.Value;
							if (!GxMotionDatabase.TryGetRandomMotionByTags(includeTags, excludeTags, minCount, out var key))
								return eState.Failure;
							Character.CrossFade(key, fadeIn, OnMotionTaskLocated);
						}
						break;

						default:
						Debug.LogError($"Unknown method: {m_Method}");
						return eState.Failure;
					}
				}
				catch (System.Exception ex)
				{
					Debug.LogError($"Failed to play animation: {ex.Message}");
					return eState.Failure;
				}
			}

			// Timeout for motion task not found.
			if (m_Initialized && m_MotionTask == null &&
				Time.timeSinceLevelLoad - m_StartTime > TIMEOUT)
			{
				Debug.LogError($"Animation {m_MotionKey.Value.ShortName} has been requested to play for more than 5 seconds.");
				return eState.Failure; // Timeout after 5 seconds
			}


			if (m_Initialized &&
				m_Style == eStyle.EnterMotionAsSuccess &&
				m_MotionTask != null)
			{
				// If the pose task is still running, we return Running state
				return eState.Success; // Pose set successfully
			}

			if (m_Initialized &&
				m_Style == eStyle.SuccessOnMotionExit &&
				m_MotionTask != null &&
				m_MotionTask.isCompleted)
			{
				return eState.Success;
			}

			if (m_Initialized &&
				m_Style == eStyle.PlayOnceAsSuccess &&
				m_MotionTask != null &&
				m_MotionTask.IsPlayedOnce())
			{
				return eState.Success;
			}
			return eState.Running;
		}

		private void OnMotionTaskLocated(GxMotionTask task)
		{
			m_MotionTask = task;
		}

		private void InternalReset()
		{
			m_Initialized = false;
			m_MotionTask = null;
			m_StartTime = 0f;
		}
		public override void OnStart()
		{
			base.OnStart();
			InternalReset();
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
