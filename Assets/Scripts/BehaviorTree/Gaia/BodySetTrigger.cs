using BehaviorDesigner.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class BodySetTrigger : OwnerBase
	{
		[Tooltip("The name of the parameter")]
		public SharedString m_Key;
		protected override eState InternalUpdate()
		{
			if (Core == null)
				return eState.Failure;

			var key = m_Key.Value;
			Core.SetTrigger(key);
			return eState.Success;
		}
	}
}