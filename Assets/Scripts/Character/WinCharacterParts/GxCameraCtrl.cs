using DesktopWizard;
using Kit2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gaia
{
    public class GxCameraCtrl : GxWinPart
	{
		[SerializeField] DwCamera m_DwCamera;
		[SerializeField] SphericalCoordinatesMono m_Helper;
		public SphericalCoordinatesMono coordinate => m_Helper;
	}

	public interface IPointerFeature : IEquatable<object>
	{
		bool isActive { get; }
		void MouseDown(GxWin ch, PointerEventData pointerEventData);
		void MouseMove(GxWin ch, PointerEventData pointerEventData);
		void MouseUp(GxWin ch, PointerEventData pointerEventData);
	}

	public interface IPointerDraggableFeature :
		IPointerFeature
	{
		bool IsHolding { get; }
		bool IsDragging { get; }

		public bool TryGetDragInfo(out GxDragInfo dragInfo);
	}
}