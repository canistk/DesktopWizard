using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	/// <summary>
	/// send the drag event to <see cref="SphericalCoordinatesMono"/>
	/// designed to be used with <see cref="GxCameraCtrl"/>
	/// and rotate the camera based on the drag event.
	/// </summary>
	public class GxDragCameraHandler : GxDraggableForm
	{
		private SphericalCoordinatesMono helper => win?.CameraCtrl?.coordinate;

		[SerializeField] float m_MaxSpeed = 1.0f;

		[SerializeField] bool m_FlipX = false;
		[SerializeField] bool m_FlipY = false;

		bool m_Inited;
		float m_TargetPolar;
		float m_TargetElevation;
		float m_T01;

		private void Update()
		{
			if (!m_Inited)
			{
				// Delay init, use coordinate init position values.
				var h = win?.CameraCtrl?.coordinate;
				if (h == null)
					return;
				m_TargetElevation = h.elevation;
				m_TargetPolar = h.polar;
				m_Inited = true;
			}

			if (m_T01 >= 1f)
				return;

			var t = Time.deltaTime * m_MaxSpeed;
			m_T01 += t;
			if (m_T01 > 1f)
				m_T01 = 1f;
			helper.polar = Mathf.LerpUnclamped(helper.polar, m_TargetPolar, m_T01);
			helper.elevation = Mathf.LerpUnclamped(helper.elevation, m_TargetElevation, m_T01);

			//helper.polar = m_TargetPolar;
			//helper.elevation = m_TargetElevation;
		}

		protected override void OnStartDrag(GxModelView ch, GxPointerEventData evt)
		{
			// Debug.Log($"{evt.button} On Start Drag");
			m_TargetElevation = helper.elevation;
			m_TargetPolar = helper.polar;
		}

		protected override void OnDragging(GxModelView ch, GxPointerEventData evt)
		{	
			m_TargetPolar += m_FlipX ?
				-evt.delta.x :
				evt.delta.x;

			m_TargetElevation += m_FlipY ?
				-evt.delta.y :
				evt.delta.y;

			m_T01 = 0f;
		}

		protected override void OnEndDrag(GxModelView ch, GxPointerEventData evt)
		{
		}
	}
}