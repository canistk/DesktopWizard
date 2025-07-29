using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Is Mouse Hover Window")]
	[TaskDescription("Check if mouse hover within the window.")]
	public class IsMouseHover : WinConditional
	{
		[SerializeField] SharedBool m_NoEventAsFailure = false;
		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwForm == null)
				return eState.Failure;

			if (ModelView.dwForm.Focused)
				return eState.Success;

			return m_NoEventAsFailure.Value ?
				eState.Failure :
				eState.Running;
		}
	}
}