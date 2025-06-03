using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
namespace Gaia
{
	public enum eState
	{
		Failure = (int)TaskStatus.Failure,
		Success = (int)TaskStatus.Success,
		Running = (int)TaskStatus.Running,
	}

    public abstract class ConditionalBase : Conditional
	{
		public sealed override TaskStatus OnUpdate()
			=> (TaskStatus)InternalUpdate();

		protected abstract eState InternalUpdate();
	}
}