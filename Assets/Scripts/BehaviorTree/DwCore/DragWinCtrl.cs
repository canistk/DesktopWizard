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
	[TaskName("Drag Win Control")]
	[TaskDescription("Drag Windows position based on drag.")]
	public class DragWinCtrl : WinAction
	{
		[SerializeField] PointerEventData.InputButton m_DetectButton = PointerEventData.InputButton.Left;

		[Header("Processing Data")]
		[RequiredField]
		public SharedDragInfo m_DragInfo;
		private DragInfo dragInfo
		{
			get
			{
				if (m_DragInfo.Value == null)
				{
					Debug.LogWarning("Non init drag info detected.");
					m_DragInfo.SetValue(new DragInfo());
				}
				return m_DragInfo.Value;
			}
		}

		[Header("Optional")]
		public SharedVector2 m_Offset = new SharedVector2();

		[SerializeField] bool m_NoEventAsFailure = false;

		public bool IsDragging => dragInfo.IsDragging;

		protected override eState OnModelViewUpdate()
		{

			if (dragInfo.ShouldEnd)
			{
				dragInfo.Reset();
				return eState.Success;
			}
			else if (!dragInfo.IsDragging)
			{
				return m_NoEventAsFailure ?
					eState.Failure :
					eState.Running;
			}
			
			// Concept, use mouse pose apply InitOffset to calculate window new position.
			var offset = dragInfo.InitOffset + (Vector3)m_Offset.Value;
			var c = ModelView.dwCamera;
			var v2i = DwCore.GetOSCursorPos();
			var cursorPos = c.MatrixOSToMonitor().MultiplyPoint3x4(new Vector3(v2i.x, v2i.y, 0f));

			var winMonPos = cursorPos - offset;
			var winOsPosf = c.MatrixMonitorToOS().MultiplyPoint3x4(winMonPos);
			var winOsPosi = new Vector2Int((int)winOsPosf.x, (int)winOsPosf.y);
			c.SetOsPos(winOsPosi);
			return eState.Running;
		}

		public override void OnAwake()
		{
			base.OnAwake();
			var c = ModelView.dwCamera;
			if (c != null)
			{
				c.EVENT_MouseDown += C_EVENT_MouseDown;
				c.EVENT_MouseUp += C_EVENT_MouseUp;
			}
		}

		public override void OnBehaviorComplete()
		{
			base.OnBehaviorComplete();
			var c = ModelView?.dwCamera;
			if (c != null)
			{
				c.EVENT_MouseDown -= C_EVENT_MouseDown;
				c.EVENT_MouseUp -= C_EVENT_MouseUp;
			}
		}

		private void C_EVENT_MouseDown(PointerEventData evt)
		{
			if (this.Disabled)
				return;
			if (evt.button != m_DetectButton)
				return;
			var c = ModelView?.dwCamera;
			dragInfo.Init(c, DwCore.GetOSCursorPos());
		}

		private void C_EVENT_MouseUp(PointerEventData evt)
		{
			if (this.Disabled)
				return;
			if (evt.button != m_DetectButton)
				return;
			if (!dragInfo.IsDragging)
				return;
			dragInfo.EndRequest();
		}
	}
}