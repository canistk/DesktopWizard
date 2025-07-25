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
				task?.FadeOut(fadeOutDuration: 0f);
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