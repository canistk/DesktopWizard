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
			var mv = win as GxWinCharacter;
			var osPos = mv.dwCamera.GetMousePosInOSSpace();
			// m_ContextMenu = GxWinCharacter.DisplayCharacterMenu(osPos.x, osPos.y, win, mv.Character);

			if (!GxWinPopup.TryDisplay("UIs/CharacterMenu",
				new Vector3(0f, 0f, -10f), osPos, new Vector2Int(300, 500),
				out var popup, out GameObject page, out var content))
			{
				Debug.LogError("Failed to create WinPopupCharacterMenu.");
				return;
			}

			if (content is not WinPopupCharacterMenu menu)
			{
				Debug.LogError("Failed to create WinPopupCharacterMenu content.");
				return;
			}

			Debug.Log($"Created WinPopupCharacterMenu: {menu.name}");
			menu.Init(win);

		}
	}
}