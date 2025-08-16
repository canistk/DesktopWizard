using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace Kit2.PieMenu
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(ImageFilledClickableSlices), typeof(RectTransform))]
	public class PieMenuItem : MonoBehaviour
    {
		public ImageFilledClickableSlices m_Image;

		private RectTransform m_RectTransform;
		private RectTransform rectTransform
		{
			get
			{
				if (m_RectTransform == null)
				{
					m_RectTransform = (RectTransform)transform;
				}
				return m_RectTransform;
			}
		}
		private void Awake()
		{
			if (m_Image == null)
				m_Image = GetComponent<ImageFilledClickableSlices>();
			m_Image.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
			m_Image.fillOrigin = 2;
			m_Image.fillClockwise = false;
		}

		public void SetAngle(float angle)
		{
			const float circle = 360f;
			var fill = angle / circle;
			m_Image.fillAmount = fill;
		}

		public void SetSize(float size)
		{
			rectTransform.sizeDelta = Vector2.one * size;
		}

	}
}