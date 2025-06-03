using DesktopWizard;
using Kit2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gaia
{
    public class GxCameraCtrl : GxModelPart
	{
		[SerializeField] DwCamera m_DwCamera;
		[SerializeField] SphericalCoordinatesMono m_Helper;
		public SphericalCoordinatesMono coordinate => m_Helper;
	}

	public interface IPointerFeature : IEquatable<object>
	{
		bool isActive { get; }
		void MouseDown(GxModelView ch, PointerEventData pointerEventData);
		void MouseMove(GxModelView ch, PointerEventData pointerEventData);
		void MouseUp(GxModelView ch, PointerEventData pointerEventData);
	}

	public interface IPointerDraggableFeature :
		IPointerFeature
	{
		bool IsHolding { get; }
		bool IsDragging { get; }

		public bool TryGetDragInfo(out GxDragInfo dragInfo);
	}
}