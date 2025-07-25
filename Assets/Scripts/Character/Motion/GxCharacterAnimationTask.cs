using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	// Base class for character animation control task.
	public abstract class GxCharacterAnimationTask : GxCharacterTask, IRetarget
	{
		public GxCharacterAnimationTask(GxCharacter character) : base(character) { }

		public abstract float GetWeight01();
		public abstract GxRetargeting GetTarget();
		public abstract void OnWillPlayAnimation(IRetarget other);
	}

	
}