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

		public override void SetAutoFadeOut(bool autoFadeOut)
		{
			this.m_AutoFadeOut = autoFadeOut;
		}
		public override void OnWillPlayAnimation(IRetarget other)
		{
			if (!m_AutoFadeOut)
				return;
			if (task.state < GxMotionTask.eState.BlendingOut)
				task?.FadeOut(fadeOutDuration: 0f);
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