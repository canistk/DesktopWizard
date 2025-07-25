using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public abstract class GxMotionHandler : ISelfDespawnable
	{
		public readonly GxMotionKey key;
		public readonly GxMotionData motionData;
		public GxCharacter Character => task?.Character;
		public GxMotionTask task { get; private set; } = null;

		public GxMotionHandler(GxMotionKey key)
		{
			this.key = key;
			if (!GxMotionDatabase.TryGetMotion(key, out motionData))
			{
				Debug.LogError($"Motion data not found for key: {key}");
			}
		}
		internal void SetTask(GxMotionTask task)
		{
			this.task = task;
		}

		public abstract void SetAutoFadeOut(bool autoFadeOut);

		public abstract GxRetargeting GetRetargeting();

		public abstract void OnInitialize();

		public abstract void Update();

		public abstract void OnWillPlayAnimation(IRetarget other);

		public abstract void SelfDespawn();
	}
}