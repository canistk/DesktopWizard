using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Kit2.PieMenu
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(ImageFilledClickableSlices), typeof(RectTransform))]
	public class PieMenuItem : MonoBehaviour, 
		IPointerEnterHandler,
		IPointerExitHandler,
		IPointerClickHandler,
		IPointerDownHandler, IPointerUpHandler
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

		[SerializeField] RectTransform m_IconWrapper;
		[SerializeField] RectTransform m_Offset;
		[SerializeField] Image m_Icon;

		[SerializeField] float m_Fade = 0.1f;
		[SerializeField] Color m_NormalColor = Color.white;
		[SerializeField] Color m_HoverColor = Color.white;
		[SerializeField] Color m_PressColor = Color.white;

		private void Awake()
		{
			if (m_Image == null)
				m_Image = GetComponent<ImageFilledClickableSlices>();
			m_Image.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
			m_Image.fillOrigin = 2;
			m_Image.fillClockwise = true;
			m_Image.color = m_NormalColor;
		}

		public void Editor_Preview(float angleStart, float angle, float size)
		{
			SetAngle(angleStart, angle);
			SetSize(size);
		}

		public void SetAngle(float angleStart, float angle)
		{
			var localRot = Quaternion.Euler(0f, 0f, angleStart);
			transform.SetLocalPositionAndRotation(Vector3.zero, localRot);

			const float circle = 360f;
			var fill = angle / circle;
			m_Image.fillAmount = fill;
		}

		public void SetIconParams(float iconDistance, float iconSlerp, float size)
		{
			var angleStart = transform.rotation.eulerAngles.z;
			var angle = m_Image.fillAmount * 360f;
			if (m_IconWrapper != null)
			{
				var eulerZ = Mathf.LerpAngle(0f, -angle, Mathf.Clamp01(iconSlerp));
				m_IconWrapper.localRotation = Quaternion.Euler(0f, 0f, eulerZ);
				m_IconWrapper.localPosition = Vector3.zero;
			}
			if (m_Offset)
			{
				var subPos = new Vector3(0f, iconDistance, 0f);
				m_Offset.localPosition = subPos;
				m_Offset.rotation = Quaternion.identity;
				m_Offset.sizeDelta = Vector2.one * size;
			}
			if (m_Icon)
			{
				m_Icon.rectTransform.sizeDelta = Vector2.one * size;
			}
		}

		public void SetIcon(Sprite sprite)
		{
			var hasIcon = sprite != null;
			if (hasIcon)
			{
				m_Icon.sprite = sprite;
			}
			if (m_Offset)
			{
				m_Offset.gameObject.SetActive(hasIcon);
			}
			if (m_Icon)
			{
				m_Icon.gameObject.SetActive(hasIcon);
			}
		}

		public void SetSize(float size)
		{
			rectTransform.sizeDelta = Vector2.one * size;
		}

		public void SetCallback(
			System.Action<PieMenuItem> enter,
			System.Action<PieMenuItem> exit,
			System.Action<PieMenuItem> clicked)
		{
			m_Enter = enter;
			m_Exit = exit;
			m_Clicked = clicked;
		}

		private System.Action<PieMenuItem> m_Enter, m_Exit, m_Clicked;

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (m_Enter != null)
				m_Enter.TryCatchDispatchEventError(o => o.Invoke(this));
			if (m_Image)
				m_Image.CrossFadeColor(m_HoverColor, m_Fade, true, true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (m_Exit != null)
				m_Exit.TryCatchDispatchEventError(o => o.Invoke(this));
			if (m_Image)
				m_Image.CrossFadeColor(m_NormalColor, m_Fade, true, true);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (m_Clicked != null)
				m_Clicked.TryCatchDispatchEventError(o => o.Invoke(this));
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (m_Image)
				m_Image.CrossFadeColor(m_PressColor, m_Fade, true, true);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (m_Image)
				m_Image.CrossFadeColor(m_NormalColor, m_Fade, true, true);
		}

		#region Button Data
		private ButtonData m_ButtonData;
		public ButtonData GetData()
		{
			return m_ButtonData;
		}

		public void SetData(ButtonData buttonData)
		{
			m_ButtonData = buttonData;
		}
		#endregion Button Data
	}
}