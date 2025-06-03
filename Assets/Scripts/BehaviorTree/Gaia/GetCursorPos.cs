using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Get Cursor Pos (ModelSpace).")]
	[TaskDescription("Get Cursor Pos.")]
	public class GetCursorPos : OwnerBase
	{
		[Header("Cursor Pos - Output")]
		[SerializeField] SharedVector2Int m_OS_Pos;
		[SerializeField] SharedVector2 m_Monitor_Pos;
		[SerializeField] SharedVector2 m_Model_Pos;

		[Header("Z Depth Override")]
		[SerializeField] float		m_ZDepthOverride = 0f;
		[SerializeField] SharedVector3 m_CursorPosInModelSpace;
		protected override eState InternalUpdate()
		{
			if (Core == null || Core.camera == null)
				return eState.Failure;
			var c		= Core.camera;
			var os		= c.GetMousePosInOSSpace();
			var world	= c.MatrixOSToMonitor().MultiplyPoint3x4(new Vector3(os.x, os.y, 0f));
			var model	= c.MonitorToModelPoint((Vector3)world);
			var origin	= (Vector3)model;
			var dir		= c.transform.forward;

			m_OS_Pos		.SetValue(os);
			m_Monitor_Pos	.SetValue((Vector2)world);
			m_Model_Pos		.SetValue((Vector2)model);
			var rst		= origin + (dir * m_ZDepthOverride);
			m_CursorPosInModelSpace.SetValue(rst);
			return eState.Success;
		}
	}
}