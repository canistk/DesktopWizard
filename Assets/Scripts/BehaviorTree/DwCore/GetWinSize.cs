using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Get Window Size")]
	[TaskDescription("Get Window Size.")]
	public class GetWinSize : WinAction
	{
		[Header("Window Size - Output")]
		[SerializeField] SharedVector2Int m_OS_Size;
		[SerializeField] SharedVector2 m_Monitor_Size;
		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwCamera == null)
				return eState.Failure;
			var c = ModelView.dwCamera;
			if (!m_OS_Size.IsNone)
			{
				var os = c.GetOsSize();
				m_OS_Size.SetValue(os);
			}
			else if (!m_Monitor_Size.IsNone)
			{
				var monSize = c.GetMonitorSize();
				m_Monitor_Size.SetValue(monSize);
			}
			return eState.Success;
		}
	}
}