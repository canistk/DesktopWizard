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
			vrma.Animation.Play();
		}

		public override void Update() { }

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