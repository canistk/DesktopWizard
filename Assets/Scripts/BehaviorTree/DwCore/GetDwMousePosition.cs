using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Get DwMouse Position")]
	[TaskDescription("Get Mouse Position via DwCore system.")]
	public class GetDwMousePosition : ActionBase
	{
		[RequiredField]
		public SharedDwCamera dwCamera;
		public SharedVector3 osSpace, monitorSpace, formSpace;

		public SharedFloat modelSpaceRayDistance = 1f;
		public SharedVector3 modelSpace;
		protected override eState InternalUpdate()
		{
			if (dwCamera.IsNone)
				return eState.Failure;

			var _dwCamera = dwCamera.Value;
			//var form = _dwCamera.Form;
			//var win = _dwCamera.dwWindow;

			if (osSpace.IsShared)		osSpace.Value		= ToV3(_dwCamera.GetMousePosInOSSpace());
			if (monitorSpace.IsShared)	monitorSpace.Value	= ToV3(_dwCamera.GetMousePosInMonitorSpace());
			if (formSpace.IsShared)		formSpace			= ToV3(_dwCamera.GetMousePosInFormSpace());

			if (modelSpace.IsShared)
			{
				var dis = modelSpaceRayDistance.Value;
				var ray = _dwCamera.GetMouseRayInModelSpace();
				modelSpace.Value = ray.origin + ray.direction * dis;
			}

			//osSpace.Value = DwCore.GetOSCursorPos();
			return eState.Success;
		}

		private Vector3 ToV3(Vector2Int o) => new Vector3(o.x, o.y, 0f);
		private Vector3 ToV3(Vector2 o) => new Vector3(o.x, o.y, 0f);
	}
}