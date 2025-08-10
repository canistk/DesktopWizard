using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("GetCursorClicked")]
	[TaskDescription("Return position when user click on Windows.")]
	public class GetCursorClicked : WinConditional
	{
		[SerializeField] PointerEventData.InputButton m_DetectButton = PointerEventData.InputButton.Left;
		[SerializeField] eEvent m_Event = eEvent.Down;
		private enum eEvent
		{
			Down,
			Up,
		}

		[SerializeField] SharedVector2 m_cursorOSPos = new SharedVector2();
		[SerializeField] SharedVector2 m_cursorMonitorPos = new SharedVector2();

		[SerializeField] SharedBool m_NoEventAsFailure = false;

		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwCamera == null)
				return eState.Failure;
			
			if (m_clickInfo.IsClicked)
			{
				if (!m_cursorOSPos.IsNone)
					m_cursorOSPos.Value = m_clickInfo.OS_Pos;
				if (!m_cursorMonitorPos.IsNone)
					m_cursorMonitorPos.Value = m_clickInfo.Monitor_Pos;
				m_clickInfo.Reset();
				return eState.Success;
			}

			return m_NoEventAsFailure.Value ?
				eState.Failure :
				eState.Running;
		}

		private class ClickInfo
		{
			public bool IsClicked;
			public Vector2Int OS_Pos;
			public Vector2 Monitor_Pos;

			public void Reset()
			{
				IsClicked = false;
				OS_Pos = Vector2Int.zero;
				Monitor_Pos = Vector2.zero;
			}
		}
		private ClickInfo m_clickInfo = new ClickInfo();

		public override void OnStart()
		{
			base.OnStart();
			m_clickInfo = new ClickInfo();
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

		private void C_EVENT_MouseUp(PointerEventData evt)
		{
			if (m_Event != eEvent.Up)
				return;
			if (evt.button != m_DetectButton)
				return;
			var c = ModelView?.dwCamera;
			m_clickInfo.IsClicked = true;
			m_clickInfo.OS_Pos = c.GetMousePosInOSSpace();
			m_clickInfo.Monitor_Pos = c.GetMousePosInMonitorSpace();
		}

		private void C_EVENT_MouseDown(PointerEventData evt)
		{
			if (m_Event != eEvent.Down)
				return;
			if (evt.button != m_DetectButton)
				return;
			var c = ModelView?.dwCamera;
			m_clickInfo.IsClicked = true;
			m_clickInfo.OS_Pos = c.GetMousePosInOSSpace();
			m_clickInfo.Monitor_Pos = c.GetMousePosInMonitorSpace();
		}
	}
}