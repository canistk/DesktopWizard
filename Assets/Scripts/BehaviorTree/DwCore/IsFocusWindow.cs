using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Is Focus Window")]
	[TaskDescription("Check if focus on a window title with the specified name.")]
	public class IsFocusWindow : WinAction
	{
		[SerializeField] SharedBool m_NoEventAsFailure = false;
		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null ||
				ModelView.dwForm == null)
				return eState.Failure;
			
			if (ModelView.dwForm.Focused)
				return eState.Success;

			return m_NoEventAsFailure.Value ?
				eState.Failure :
				eState.Running;
		}
	}
}