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
				throw new System.Exception($"Pose data not found for key: {poseKey}");

			var enterTracks = poseData.GetEnterTracks().ToArray();
			if (enterTracks.Length == 0) throw new System.Exception($"Pose data for key '{poseKey}' has no enter tracks defined.");
			var loopTracks = poseData.GetLoopTracks().ToArray();
			if (loopTracks.Length == 0) throw new System.Exception($"Pose data for key '{poseKey}' has no loop tracks defined.");
			var exitTracks = poseData.GetExitTracks().ToArray();
			if (exitTracks.Length == 0) throw new System.Exception($"Pose data for key '{poseKey}' has no exit tracks defined.");

			enterKey = enterTracks[Random.Range(0, enterTracks.Length)];
			loopKey = loopTracks[Random.Range(0, loopTracks.Length)];
			exitKey = exitTracks[Random.Range(0, exitTracks.Length)];
			m_State = eState.None;
		}

		public override GxRetargeting GetTarget() => null;

		public override float GetWeight01() => 0f;

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
						var duration = exitTask?.Handler?.motionData?.ClipLength ?? 0f;
						exitTask.FadeOut(duration * 0.8f);
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
					// not yet exit
					if (exitKey.Equals(GxMotionKey.Invalid))
					{
						Abort(); // No exit key, just complete the task
					}
					else
					{
						// Try enter exit animation
						m_LastWaitTime = Time.realtimeSinceStartup;
						Character.CrossFade(exitKey, 0f, DisableAutoFadeOut);
					}
				}
			}
		}

		private enum eState
		{
			None,
			Wait4Previous,
			Wait4Enter,
			Entering,
			Wait4Loop,
			Looping,
			Wait4Exit,
			Exiting,
			FadeOut,
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
		private void Loop(GxMotionTask task)
		{
			if (task != null && task.Handler != null)
			{
				task.Handler.SetAutoFadeOut(false);
				if (task.Handler is GxTimelineHandler handler)
				{
					var pd = handler.timeline.playableDirector;
					pd.extrapolationMode = UnityEngine.Playables.DirectorWrapMode.Loop;
				}
			}
		}

		private const float TIMEOUT = 1f;
		private float m_LastWaitTime = 0f;
		protected override bool InternalExecute()
		{
			switch (m_State)
			{
				case eState.None:
				{
					Character.BoardcastWillPlayAnimation(this);
					m_State = eState.Wait4Previous;
					m_LastWaitTime = Time.realtimeSinceStartup;
				}
				return true;

				case eState.Wait4Previous:
				{
					bool other = false;
					foreach (var t in Character.GetActiveAnimations())
					{
						if (t is GxPoseTask pt && pt != this)
						{
							other = true;
							break;
						}
					}
					var waitTooLong = Time.realtimeSinceStartup - m_LastWaitTime > TIMEOUT;
					if (!other || waitTooLong)
					{
						m_State = eState.Wait4Enter;
						m_LastWaitTime = Time.realtimeSinceStartup;
						Character.CrossFade(enterKey, 0.25f, DisableAutoFadeOut);
					}
				}
				return true;
				
				case eState.Entering:
				if (enterTask != null && enterTask.IsPlayedOnce())
				{
					m_State = eState.Wait4Loop;
					m_LastWaitTime = Time.realtimeSinceStartup;
					Character.CrossFade(loopKey, 0f, Loop);
				}
				return true;
				case eState.Looping:
				if (loopTask != null && loopTask.isCompleted)
				{
					m_State = eState.Wait4Exit;
					m_LastWaitTime = Time.realtimeSinceStartup;
					Character.CrossFade(exitKey, 0f, DisableAutoFadeOut);
				}
				return true;
				case eState.Exiting:
				if (exitTask != null && exitTask.IsPlayedOnce())
				{
					m_State = eState.Completed;
					return false;
				}
				return true;

				case eState.Wait4Enter:
				case eState.Wait4Loop:
				case eState.Wait4Exit:
				{
					var waitTooLong = Time.realtimeSinceStartup - m_LastWaitTime > TIMEOUT;
					if (waitTooLong)
					{
						Debug.LogWarning($"Pose task '{poseData.key}' is waiting too long in state '{m_State}'. Aborting.");
						++m_State;
					}
					return true; // Continue waiting
				}

				case eState.Completed:
				default:
					return false; // Task completed
			}
		}

	}
}