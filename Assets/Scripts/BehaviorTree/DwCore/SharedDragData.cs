using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorDesigner.Runtime
{
	[System.Serializable]
	public class DragData
	{
		public Vector2Int cursorOsPos;
		public Vector3 InitCursorMonitorPos;
		public Vector3 InitWinMonitorPos;
		public Vector3 InitOffset;
		public Matrix4x4 o2m;
		public Matrix4x4 m2f;


		[SerializeField] bool m_ShouldEnd;
		public bool ShouldEnd => m_ShouldEnd;
		[SerializeField] bool m_IsDragging;
		public bool IsDragging => m_IsDragging && !ShouldEnd;

		private DwCamera m_Camera;
		public void Init(DwCamera camera, Vector2Int osCursorPos)
		{
			if (camera == null)
				throw new System.NullReferenceException();
			this.m_Camera = camera;
			this.cursorOsPos = osCursorPos;
			this.o2m = this.m_Camera.MatrixOSToMonitor();
			this.m2f = this.m_Camera.MatrixMonitorToForm();
			this.InitCursorMonitorPos = o2m.MultiplyPoint3x4(new Vector3(osCursorPos.x, osCursorPos.y, 0f));
			this.InitWinMonitorPos = o2m.MultiplyPoint3x4(new Vector3(m_Camera.Left, m_Camera.Top, 0f));
			this.InitOffset = InitCursorMonitorPos - InitWinMonitorPos;
			this.m_IsDragging = true;
		}

		public void EndRequest()
		{
			if (!IsDragging)
				return;
			m_ShouldEnd = true;
		}

		public void Reset()
		{
			this.m_IsDragging = false;
			this.m_ShouldEnd = false;
			this.cursorOsPos = default;
			this.InitCursorMonitorPos = this.InitWinMonitorPos = this.InitOffset = default;
			this.o2m = this.m2f = Matrix4x4.identity;
		}
	}

	[System.Serializable]
	public class SharedDragData : SharedVariable<DragData>
	{
		public static implicit operator SharedDragData(DragData value) { return new SharedDragData { mValue = value }; }
	}
}
