using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{

	public class GxTimelineHandler : GxMotionHandler, IDisposable
	{
		public GxTimelineAsset timeline;
		public float fadeIn;
		private bool m_AutoFadeOut = true;
		private float fadeOut = 0.25f;
		public GxTimelineHandler(GxMotionKey key, GxTimelineAsset timeline, float fadeIn) : base(key)
		{
			this.timeline = timeline;
			this.fadeIn = fadeIn;
			this.m_AutoFadeOut = true;
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
		public override void SetAutoFadeOut(bool autoFadeOut, float duration = 0.25f)
		{
			this.m_AutoFadeOut = autoFadeOut;
		}

		public bool IsLoop()
		{
			if (timeline == null || timeline.playableDirector == null)
				return false;
			return timeline.playableDirector.extrapolationMode == UnityEngine.Playables.DirectorWrapMode.Loop;
		}

		public void SetLoop(bool loop)
		{
			if (timeline == null || timeline.playableDirector == null)
				return;
			timeline.playableDirector.extrapolationMode = loop ?
				UnityEngine.Playables.DirectorWrapMode.Loop :
				UnityEngine.Playables.DirectorWrapMode.Hold;
		}

		public override void OnWillPlayAnimation(IRetarget other)
		{
			if (!m_AutoFadeOut)
				return;
			if (other is not GxMotionTask target)
				return; // ignore group or other types of retargets
				
			if (task.state < GxMotionTask.eState.BlendingIn)
				task.Abort(); // Abort current task if not yet blending in
			if (task.state >= GxMotionTask.eState.BlendingOut)
				return; // Already blending out, do nothing
			task?.FadeOut(Mathf.Max(0f, fadeOut));
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
}