using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	public class GxContentMenu : GxClickableBase
	{
		protected override void InternalMouseDown(GxWin ch, PointerEventData pointerEvent)
		{
			//if (IsShowContextMenu)
			//	return;
			InternalShowContextMenu();
		}

		protected override void InternalMouseUp(GxWin ch, PointerEventData pointerEvent)
		{
			
		}

		private WinPopupCharacterMenu m_ContextMenu;
		public bool IsShowContextMenu => m_ContextMenu != null && m_ContextMenu.gameObject.activeSelf;

		private void InternalShowContextMenu()
		{
			var mv = modelView as GxWinCharacter;
			var osPos = mv.dwCamera.GetMousePosInOSSpace();
			m_ContextMenu = GxWinCharacter.DisplayCharacterMenu(osPos.x, osPos.y, modelView, mv.Character);
		}
	}
}