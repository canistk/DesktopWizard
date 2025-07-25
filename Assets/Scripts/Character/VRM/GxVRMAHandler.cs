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
		public GxVRMAHandler(GxMotionKey key, GxVRMAToken vrma, float fadeIn) : base(key)
		{
			this.vrma = vrma;
		}

		public override GxRetargeting GetRetargeting() => vrma?.Retargeting;

		public override void OnInitialize()
		{
			// vrma.gltf.ShowMeshes();
			vrma.Vrm10AnimationInstance.ShowBoxMan(false);
			vrma.Animation.Play();
		}

		public override void Update()
		{
		}

		public override void OnWillPlayAnimation(IRetarget other)
		{
			if (task.state < GxMotionTask.eState.BlendingOut)
				task?.FadeOut(fadeOutDuration: 0f, realTime: true);
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