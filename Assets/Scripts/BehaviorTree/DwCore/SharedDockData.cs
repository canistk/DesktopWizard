using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using Obi;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.EventSystems;
namespace BehaviorDesigner.Runtime
{
    [System.Serializable]
    public class DockData
    {
        /// <summary>The window space</summary>
        public SharedOSRect winRect;

        /// <summary>The actual position offset based on Pivot point in monitor space</summary>
        public Vector2 Offset;

        // x : -1 Left      ~ 1 Right
        // y : -1 Bottom    ~ 1 Top
        // z : -1 Rear      ~ 1 Forward
        /// <summary>
        /// Relative percentage within the OSRect space Range -1 to 1
        /// </summary>
        public Vector2 Pivot;

        // TODO: Case 1
        // WinRect moved, calculate related Pivot + Offset
        public Vector2 CalculatePivotPoint(DwCamera dwCamera)
        {
			var orgPivot = CalculatePivot2MonitorPoint(dwCamera);
			var rst = orgPivot + Offset;
            return rst;
		}

        // TODO: Case 2
        // position in monitor space, calculate related winRect based on Pivot + Offset ++ winRect
        public OSRect CalculateModelViewByPoint(DwCamera dwCamera, Vector2 monitorPosition)
        {
            //var width           = Mathf.Abs(monitorLB.x - monitorRT.x);
            //var height          = Mathf.Abs(monitorLB.y - monitorRT.y);
            var pivotMonPos = CalculatePivotPoint(dwCamera);
			var diff = pivotMonPos - monitorPosition;
            var m2o = dwCamera.MatrixMonitorToOS();
            var osDiff = m2o.MultiplyPoint3x4((Vector3)diff);
            var _x = (int)osDiff.x;
            var _y = (int)osDiff.y;

            var org = dwCamera.rect;
            var rst = new OSRect {
                Left    = org.Left      + _x,
                Top     = org.Top       + _y,
                Right   = org.Bottom    + _x,
                Bottom  = org.Bottom    + _y
            };
            return rst;
		}

        private Vector2 CalculatePivot2MonitorPoint(DwCamera dwCamera)
        {
			var r = winRect.Value;
			var o2m = dwCamera.MatrixOSToMonitor();
			var monitorLB = o2m.MultiplyPoint3x4(new Vector3(r.Left, r.Bottom, 0f));
			var monitorRT = o2m.MultiplyPoint3x4(new Vector3(r.Right, r.Top, 0f));
			
            // remap -1 ~ 1 to 0 ~ 1
			var x01 = Mathf.Clamp(Pivot.x, -1f, 1f) * 0.5f + 0.5f;
			var y01 = Mathf.Clamp(Pivot.y, -1f, 1f) * 0.5f + 0.5f;

			var x = Mathf.Lerp(monitorLB.x, monitorRT.x, x01);
			var y = Mathf.Lerp(monitorLB.y, monitorRT.y, y01);
			var anchor = new Vector2(x, y);
            return anchor;
		}
	}
    public class SharedDockData : SharedVariable<DockData>
	{
		public static implicit operator SharedDockData(DockData value) { return new SharedDockData { mValue = value }; }
	}
}