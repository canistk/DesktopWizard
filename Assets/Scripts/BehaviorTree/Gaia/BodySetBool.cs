using BehaviorDesigner.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class BodySetBool : OwnerBase
	{
		[Tooltip("The name of the parameter")]
		public SharedString m_Key;
		public SharedBool m_Value;
		protected override eState InternalUpdate()
		{
			if (Core == null)
				return eState.Failure;

			var key = m_Key.Value;
			var value = m_Value.Value;
			Core.SetBool(key, value);
			return eState.Success;
		}
	}
}
