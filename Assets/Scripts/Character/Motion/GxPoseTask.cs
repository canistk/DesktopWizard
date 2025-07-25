using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Gaia
{
    public class GxPoseTask : GxCharacterAnimationTask, IRetarget
	{
		private GxPoseData poseData;
		private GxMotionKey enterKey, loopKey, exitKey;
		private GxMotionTask enterTask, loopTask, exitTask;

		public GxPoseTask(string poseKey, GxCharacter character) : base(character)
		{
			if (!GxMotionDatabase.TryGetPose(poseKey, out poseData))
			{
				Debug.LogError($"Pose data not found for key: {poseKey}");
				return;
			}

			var enterTracks = poseData.GetEnterTracks().ToArray();
			var loopTracks = poseData.GetLoopTracks().ToArray();
			var exitTracks = poseData.GetExitTracks().ToArray();
			enterKey = enterTracks[Random.Range(0, enterTracks.Length)];
			loopKey = loopTracks[Random.Range(0, loopTracks.Length)];
			exitKey = exitTracks[Random.Range(0, exitTracks.Length)];
			m_State = eState.None;
		}

		public override GxRetargeting GetTarget()
		{
			if (isDisposed || isCompleted)
				return null; // Task is not active
			if (m_State < eState.Entering)
				return null; // Not yet in a valid state
			if (m_State == eState.Entering && enterTask != null)
				return enterTask.GetTarget();
			if (m_State == eState.Looping && loopTask != null)
				return loopTask.GetTarget();
			if (m_State == eState.Exiting && exitTask != null)
				return exitTask.GetTarget();
			return null; // Default case, not in a valid state
		}

		public override float GetWeight01()
		{
			if (isDisposed || isCompleted)
				return 0f; // Task is not active
			if (m_State < eState.Entering)
				return 0f; // Not yet in a valid state
			if (m_State == eState.Entering && enterTask != null)
				return enterTask.GetWeight01();
			if (m_State == eState.Looping && loopTask != null)
				return loopTask.GetWeight01();
			if (m_State == eState.Exiting && exitTask != null)
				return exitTask.GetWeight01();
			return 0f; // Default case, not in a valid state
		}

		public override void OnWillPlayAnimation(IRetarget other)
		{
			// Pose should only care other pose.
			if (other is GxMotionTask task)
			{
				switch (m_State)
				{
					case eState.Wait4Enter:
					if (task.Key.Equals(enterKey))
					{
						enterTask = task;
						m_State = eState.Entering;
					}
					break;
					case eState.Wait4Loop:
					if (task.Key.Equals(loopKey))
					{
						loopTask = task;
						m_State = eState.Looping;
					}
					break;
					case eState.Wait4Exit:
					if (task.Key.Equals(exitKey))
					{
						exitTask = task;
						m_State = eState.Exiting;
					}
					break;
				}
			}

			if (m_State < eState.Wait4Exit &&
				other is GxPoseTask otherPose &&
				otherPose != this)
			{
				// another pose is about to play, exit current pose
				// force exit if we are not in the exiting state
				m_State = eState.Wait4Exit;
				if (enterTask != null && !enterTask.isCompleted)
					enterTask.Abort();
				if (loopTask != null && !loopTask.isCompleted)
					loopTask.Abort();
				if (exitTask == null)
				{
					if (exitKey.Equals(GxMotionKey.Invalid))
					{
						Abort(); // No exit key, just complete the task
						return;
					}
					else
					{
						// Try enter exit animation
						Character.CrossFade(exitKey, 0f, DisableAutoFadeOut);
					}
				}
			}
		}

		private enum eState
		{
			None,
			Wait4Enter,
			Entering,
			Wait4Loop,
			Looping,
			Wait4Exit,
			Exiting,
			Completed,
		}
		private eState m_State = eState.None;

		private void DisableAutoFadeOut(GxMotionTask task)
		{
			if (task != null && task.Handler != null)
			{
				task.Handler.SetAutoFadeOut(false);
			}
		}

		protected override bool InternalExecute()
		{
			switch (m_State)
			{
				case eState.None:
				{
					m_State = eState.Wait4Enter;
					Character.CrossFade(enterKey, 0.25f, DisableAutoFadeOut);
				}
				return true;
				
				case eState.Entering:
				if (enterTask == null || enterTask.isCompleted)
				{
					m_State = eState.Wait4Loop;
					// enterTask = null;
					Character.CrossFade(loopKey, 0f, DisableAutoFadeOut);
				}
				return true;
				case eState.Looping:
				if (loopTask == null || loopTask.isCompleted)
				{
					m_State = eState.Wait4Exit;
					// loopTask = null;
					Character.CrossFade(exitKey, 0f, DisableAutoFadeOut);
				}
				return true;
				case eState.Exiting:
				if (exitTask == null || exitTask.isCompleted)
				{
					m_State = eState.Completed;
					return false;
				}
				return true;

				case eState.Wait4Enter:
				case eState.Wait4Loop:
				case eState.Wait4Exit:
				{
					return true; // Continue waiting
				}

				case eState.Completed:
				default:
					return false; // Task completed
			}
		}

	}
}