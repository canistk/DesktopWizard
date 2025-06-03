using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Set Look At")]
	[TaskDescription("Set Avatar's look at Pos in (ModelSpace).")]
	public class SetLookAtTargetPos : OwnerBase
	{
        [SerializeField] SharedVector3 m_TargetPos;

		protected override eState InternalUpdate()
		{
			if (Core == null)
				return eState.Failure;
			var v3 = m_TargetPos.Value;
			Core.SetLookAtTargetPos(v3);
			return eState.Success;
		}
	}
}