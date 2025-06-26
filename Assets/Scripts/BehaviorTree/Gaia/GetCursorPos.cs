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
	public class GetCursorPos : ModelViewBase
	{
		[Header("Cursor Pos - Output")]
		[SerializeField] SharedVector2Int m_OS_Pos;
		[SerializeField] SharedVector2 m_Monitor_Pos;
		[SerializeField] SharedVector2 m_Model_Pos;

		[Header("Z Depth Override")]
		[SerializeField] float		m_ZDepthOverride = 0f;
		[SerializeField] SharedVector3 m_CursorPosInModelSpace;
		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwCamera == null)
				return eState.Failure;
			var c		= ModelView.dwCamera;
			var os		= c.GetMousePosInOSSpace();
			var world	= c.GetMousePosInMonitorSpace();
			var model	= c.GetMouseRayInModelSpace();
			var origin	= model.origin;
			var dir		= c.transform.forward;

			m_OS_Pos		.SetValue(os);
			m_Monitor_Pos	.SetValue((Vector2)world);
			m_Model_Pos		.SetValue(origin);
			var rst		= origin + (dir * m_ZDepthOverride);
			m_CursorPosInModelSpace.SetValue(rst);
			return eState.Success;
		}
	}
}