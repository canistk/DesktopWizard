using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("SetWinDrag")]
	[TaskDescription("Drag Windows position based on drag.")]
	public class SetWinDrag : WinAction
	{
		[SerializeField] PointerEventData.InputButton m_DetectButton = PointerEventData.InputButton.Left;

		[Header("Monitor Space")]
		[RequiredField] public SharedVector2 m_InitCursorMonitorPos = new SharedVector2();
		[RequiredField] public SharedVector2 m_InitWindowMonitorPos = new SharedVector2();

		[Header("Optional")]
		public SharedVector2 m_Offset = new SharedVector2();

		[SerializeField] bool m_NoEventAsFailure = false;

		private struct DragInfo
		{
			public bool IsDragging;
			public bool ShouldEnd;
			public Vector2Int cursorOsPos;
			public Vector3 InitCursorMonitorPos;
			public Vector3 InitWinMonitorPos;
			public Vector3 InitOffset;
			public Matrix4x4 o2m;
			public Matrix4x4 m2f;
			public void Init(DwCamera c, Vector2Int osCursorPos)
			{
				this.cursorOsPos = osCursorPos;
				this.o2m = c.MatrixOSToMonitor();
				this.m2f = c.MatrixMonitorToForm();
				this.InitCursorMonitorPos = o2m.MultiplyPoint3x4(new Vector3(osCursorPos.x, osCursorPos.y, 0f));
				this.InitWinMonitorPos = o2m.MultiplyPoint3x4(new Vector3(c.Left, c.Top, 0f));
				this.InitOffset = InitCursorMonitorPos - InitWinMonitorPos;
			}
			public void Reset()
			{
				IsDragging = false;
				ShouldEnd = false;
				cursorOsPos = default;
				InitCursorMonitorPos = InitWinMonitorPos = InitOffset = default;
				o2m = m2f = Matrix4x4.identity;
			}
		}
		private DragInfo m_DragInfo;
		public bool IsDragging => m_DragInfo.IsDragging;

		protected override eState OnModelViewUpdate()
		{
			if (!m_DragInfo.IsDragging)
			{
				return m_NoEventAsFailure ? eState.Failure : eState.Running;
			}
			else if (m_DragInfo.ShouldEnd)
			{
				m_DragInfo.Reset();
				return eState.Success;
			}

			// Concept, use mouse pose apply InitOffset to calculate window new position.
			var offset = m_DragInfo.InitOffset + (Vector3)m_Offset.Value;
			var c = ModelView.dwCamera;
			var v2i = DwCore.GetOSCursorPos();
			var cursorPos = c.MatrixOSToMonitor().MultiplyPoint3x4(new Vector3(v2i.x, v2i.y, 0f));

			var winMonPos = cursorPos + m_DragInfo.InitOffset + (Vector3)m_Offset.Value;
			var winOsPosf = c.MatrixMonitorToOS().MultiplyPoint3x4(winMonPos);
			var winOsPosi = new Vector2Int((int)winOsPosf.x, (int)winOsPosf.y);
			c.SetOsPos(winOsPosi);
			return eState.Running;
		}

		public override void OnStart()
		{
			base.OnStart();
			m_DragInfo.Reset();
			var c = ModelView.dwCamera;
			if (c != null)
			{
				c.EVENT_MouseDown += C_EVENT_MouseDown;
				c.EVENT_MouseUp += C_EVENT_MouseUp;
			}
		}

		public override void OnEnd()
		{
			base.OnEnd();
			var c = ModelView?.dwCamera;
			if (c != null)
			{
				c.EVENT_MouseDown -= C_EVENT_MouseDown;
				c.EVENT_MouseUp -= C_EVENT_MouseUp;
			}
		}

		public override void OnReset()
		{
			base.OnReset();
			var c = ModelView?.dwCamera;
			if (c != null)
			{
				c.EVENT_MouseDown -= C_EVENT_MouseDown;
				c.EVENT_MouseUp -= C_EVENT_MouseUp;
			}
		}

		private void C_EVENT_MouseDown(PointerEventData evt)
		{
			if (evt.button != m_DetectButton)
				return;
			var c = ModelView?.dwCamera;
			m_DragInfo.Init(c, DwCore.GetOSCursorPos());

			m_InitCursorMonitorPos		.SetValue(m_DragInfo.InitCursorMonitorPos);
			m_InitWindowMonitorPos		.SetValue(m_DragInfo.InitWinMonitorPos);
		}

		private void C_EVENT_MouseUp(PointerEventData evt)
		{
			if (evt.button != m_DetectButton)
				return;
			if (!m_DragInfo.IsDragging)
				return;
			m_DragInfo.ShouldEnd = true;
		}
	}
}