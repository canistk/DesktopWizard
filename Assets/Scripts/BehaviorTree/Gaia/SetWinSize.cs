using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Set Window Size")]
	[TaskDescription("Set Window Size.")]
	public class SetWinSize : WinBase
	{
		[Header("Window Position - Input")]
		[SerializeField] SharedVector2Int m_OS_Size;
		[SerializeField] SharedVector2 m_Monitor_Size;
		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwCamera == null)
				return eState.Failure;

			var c = ModelView.dwCamera;
			if (!m_OS_Size.IsNone)
			{
				var os = new Vector2Int(c.Left, c.Top);
				c.SetOsSize(os);
			}
			else if (!m_Monitor_Size.IsNone)
			{
				var mPos = m_Monitor_Size.Value;
				c.SetMonitorSize(mPos);
			}

			return eState.Success;
		}
	}
}