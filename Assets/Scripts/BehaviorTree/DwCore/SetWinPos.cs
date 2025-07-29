using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Set Window Position")]
	[TaskDescription("Set Window Position.")]
	public class SetWinPos : WinAction
	{
		[Header("Window Position - Input")]
		[SerializeField] SharedVector2Int m_OS_Pos;
		[SerializeField] SharedVector2 m_Monitor_Pos;
		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwCamera == null)
				return eState.Failure;

			var c = ModelView.dwCamera;
			if (!m_OS_Pos.IsNone)
			{
				var os = new Vector2Int(c.Left, c.Top);
				c.SetOsPos(os);
			}
			else if (!m_Monitor_Pos.IsNone)
			{
				var mPos = m_Monitor_Pos.Value;
				c.SetMonitorPos(mPos);
			}

			return eState.Success;
		}
		public override void OnStart()
		{
			base.OnStart();
			m_OS_Pos.SetValue(default);
			m_Monitor_Pos.SetValue(default);
		}
	}
}