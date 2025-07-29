using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gaia;
namespace BehaviorDesigner.Runtime
{
    public class SharedGxMotionKey : SharedVariable<GxMotionKey>
    {
        public static implicit operator SharedGxMotionKey(GxMotionKey value)
            => new SharedGxMotionKey { mValue = value };
	}
}