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
	public class IsMouseHover : ConditionalBase
	{
		[RequiredField]
		public SharedDwCamera m_dwCamera;
		protected override eState InternalUpdate()
		{
			if (m_dwCamera.IsNone)
				return eState.Failure;
			var dwCamera = m_dwCamera.Value;
			var dwWindow = dwCamera.dwWindow;
			if (dwWindow == null)
				return eState.Failure;

			var bounds = dwWindow.GetMonitorBounds();
			var cursor = dwCamera.GetMousePosInMonitorSpace();

			if (!bounds.Contains(cursor))
				return eState.Failure;
			return eState.Success;
		}
	}
}