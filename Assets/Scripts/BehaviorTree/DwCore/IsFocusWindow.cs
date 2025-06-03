using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Is Focus Window")]
	[TaskDescription("Check if focus on a window title with the specified name.")]
	public class IsFocusWindow : ConditionalBase
	{
		[RequiredField]
		public SharedDwCamera dwCamera;
		protected override eState InternalUpdate()
		{
			if (dwCamera == null || dwCamera.IsNone)
				return eState.Failure;

			var form = dwCamera.Value.dwForm;
			if (form.Focused)
				return eState.Success;

			return eState.Failure;
		}
	}
}