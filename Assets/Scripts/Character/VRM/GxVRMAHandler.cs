using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class GxVRMAHandler : GxMotionHandler, IDisposable
	{
		public GxVRMAToken vrma;
		public float fadeIn;
		private bool m_AutoFadeOut = true;
		private float fadeOut = 0.25f;
		public GxVRMAHandler(GxMotionKey key, GxVRMAToken vrma, float fadeIn) : base(key)
		{
			this.vrma = vrma;
			this.m_AutoFadeOut = true;
		}

		public override GxRetargeting GetRetargeting() => vrma?.Retargeting;

		public override void OnInitialize()
		{
			// vrma.gltf.ShowMeshes();
			vrma.Vrm10AnimationInstance.ShowBoxMan(false);
			m_PlayedOnce = 1; // Mark as started play
			vrma.Animation.Play();
		}

		public override bool IsLoop()
		{
			if (vrma == null || vrma.Animation == null)
				return false;
			return vrma.Animation.wrapMode == WrapMode.Loop;
		}

		public override void SetLoop(bool loop)
		{
			if (vrma == null || vrma.Animation == null)
				return;
			vrma.Animation.wrapMode = loop ?
				WrapMode.Loop :
				WrapMode.Once;
		}

		private int m_PlayedOnce = 0; // 0 = not played, 1 = started play, 2 = played Once
		public override void Update()
		{
			if (!vrma.Animation.isPlaying && m_PlayedOnce == 1)
			{
				Character.BoardCastPlayedOnce(task);
				m_PlayedOnce = 2; // Mark as played once
			}
		}

		public override void SetAutoFadeOut(bool autoFadeOut, float duration = 0.25f)
		{
			this.m_AutoFadeOut = autoFadeOut;
			this.fadeOut = duration;
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
			vrma.Animation.Stop();
			vrma.SelfDespawn();
		}

		void IDisposable.Dispose()
		{
			vrma?.SelfDespawn();
			vrma = null;
		}
	}
}