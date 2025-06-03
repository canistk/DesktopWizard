using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
namespace Gaia
{
	public abstract class ActionBase : Action
	{
		public sealed override TaskStatus OnUpdate()
			=> (TaskStatus)InternalUpdate();

		protected abstract eState InternalUpdate();
	}
}