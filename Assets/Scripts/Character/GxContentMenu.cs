using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	public class GxContentMenu : GxClickableBase
	{
		protected override void InternalMouseDown(GxModelView ch, PointerEventData pointerEvent)
		{
			//if (IsShowContextMenu)
			//	return;
			InternalShowContextMenu();
		}

		protected override void InternalMouseUp(GxModelView ch, PointerEventData pointerEvent)
		{
			
		}

		private UIPopupCharacterMenu m_ContextMenu;
		public bool IsShowContextMenu => m_ContextMenu != null && m_ContextMenu.gameObject.activeSelf;

		private void InternalShowContextMenu()
		{
			var osPos = modelView.dwCamera.GetMousePosInOSSpace();
			m_ContextMenu = GxModelView.DisplayCharacterMenu(osPos.x, osPos.y, modelView, modelView.Character);
		}
	}
}