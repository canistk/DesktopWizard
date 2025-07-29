using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Get Window Position")]
	[TaskDescription("Get Window Position.")]
	public class GetWinPos : WinBase
    {
        [Header("Window Position - Output")]
        [SerializeField] SharedVector2Int m_OS_Pos;
        [SerializeField] SharedVector2 m_Monitor_Pos;
        protected override eState OnModelViewUpdate()
        {
            if (ModelView == null || ModelView.dwCamera == null)
                return eState.Failure;
            var c = ModelView.dwCamera;
            if (!m_OS_Pos.IsNone)
            {
                var os = c.GetOsPos();
                m_OS_Pos.SetValue(os);
            }
            else if (!m_Monitor_Pos.IsNone)
            {
                var monPos = c.GetMonitorPos();
                m_Monitor_Pos.SetValue(monPos);
            }
            return eState.Success;
        }
	}
}