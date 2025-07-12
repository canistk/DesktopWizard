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
	[System.Obsolete]
	public class SetLookAtTargetPos : WinBase
	{
        [SerializeField] SharedVector3 m_TargetPos;

		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null)
				return eState.Failure;
			var v3 = m_TargetPos.Value;
			// ModelView.SetLookAtTargetPos(v3);
			// TODO: Implement SetLookAtTargetPos in GxModelView
			throw new System.NotImplementedException();
			return eState.Success;
		}
	}
}