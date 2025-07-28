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
	public class GetCursorPos : WinBase
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

			if (!m_OS_Pos.IsNone)
			{
				var os		= c.GetMousePosInOSSpace();
				m_OS_Pos		.SetValue(os);
			}
			if (!m_Monitor_Pos.IsNone)
			{
				var world	= c.GetMousePosInMonitorSpace();
				m_Monitor_Pos	.SetValue((Vector2)world);
			}

			var model	= c.GetMouseRayInModelSpace();
			var origin	= model.origin;
			var dir		= c.transform.forward;
			var rst		= origin + (dir * m_ZDepthOverride);
			if (!m_Model_Pos.IsNone)
			{
				m_Model_Pos		.SetValue(origin);
			}
			if (!m_CursorPosInModelSpace.IsNone)
			{
				m_CursorPosInModelSpace.SetValue(rst);
			}
			return eState.Success;
		}
	}
}