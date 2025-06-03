using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	public class GxContentMenu : GxClickableBase
	{
		[SerializeField] private DwCamera m_ContextMenu;
		protected override void InternalMouseDown(GxModelView ch, PointerEventData pointerEvent)
		{
			if (IsShowContextMenu)
				return;
			InternalShowContextMenu();
		}

		protected override void InternalMouseUp(GxModelView ch, PointerEventData pointerEvent)
		{
			
		}

		public bool IsShowContextMenu => m_ContextMenu != null && m_ContextMenu.gameObject.activeSelf;

		private void InternalShowContextMenu()
		{
			if (m_ContextMenu == null)
			{
				Debug.LogError("ContextMenu is not set.");
				return;
			}
			if (m_ContextMenu.gameObject.activeSelf)
				return;

			var osPos = win.dwCamera.GetMousePosInOSSpace();
			//Debug.Log($"ContextMenu pos = {osPos}");
			m_ContextMenu.gameObject.SetActive(true);
			m_ContextMenu.Left = osPos.x;
			m_ContextMenu.Top = osPos.y;
			m_ContextMenu.EVENT_LostFocus += InternalHideContextMenu;
			m_ContextMenu.EVENT_Closed += InternalHideContextMenu;
		}
		private void InternalHideContextMenu()
		{
			if (m_ContextMenu == null)
			{
				Debug.LogError("ContextMenu is not set.");
				return;
			}

			m_ContextMenu.EVENT_LostFocus -= InternalHideContextMenu;
			m_ContextMenu.EVENT_Closed -= InternalHideContextMenu;
			m_ContextMenu.gameObject.SetActive(false);
		}
	}
}