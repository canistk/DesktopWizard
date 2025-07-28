using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{

    public class GetWinPos : WinBase
    {
        [Header("Window Position - Output")]
        [SerializeField] SharedVector2Int m_OS_Pos;
        [SerializeField] SharedVector2 m_Monitor_Pos;
        // [SerializeField] SharedVector2 m_Model_Pos;
        protected override eState OnModelViewUpdate()
        {
            if (ModelView == null || ModelView.dwCamera == null)
                return eState.Failure;
            var c = ModelView.dwCamera;
            if (!m_OS_Pos.IsNone)
            {
                var os = new Vector2Int(c.Left, c.Top);
                m_OS_Pos.SetValue(os);
            }
            else if (!m_Monitor_Pos.IsNone)
            {
                var world = c.GetMousePosInMonitorSpace();
                m_Monitor_Pos.SetValue((Vector2)world);
            }
            //else if (!m_Model_Pos.IsNone)
            //{
            //    var model = c.GetMouseRayInModelSpace();
            //    var origin = model.origin;
            //    m_Model_Pos.SetValue(origin);
            //}
            return eState.Success;
        }
	}
}