using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	public abstract class GxClickableBase : GxMouseBase, IPointerFeature
	{

		[SerializeField] private PointerEventData.InputButton m_Button = PointerEventData.InputButton.Left;
		protected PointerEventData.InputButton GetButtonRef() => m_Button;

		protected virtual bool IsConditionPass(PointerEventData pointerEventData)
		{
			return pointerEventData.button == GetButtonRef();
		}

		bool IPointerFeature.isActive => gameObject.activeInHierarchy;

		void IPointerFeature.MouseDown(GxModelView ch, PointerEventData pointerEvent)
		{
			if (!IsConditionPass(pointerEvent))
				return;
			InternalMouseDown(ch, pointerEvent);
		}
		protected abstract void InternalMouseDown(GxModelView ch, PointerEventData pointerEvent);

		void IPointerFeature.MouseMove(GxModelView ch, PointerEventData pointerEventpointerEvent)
		{}
		// => protected abstract void InternalMouseMove(GxModelView ch, PointerEventData pointerEvent);

		void IPointerFeature.MouseUp(GxModelView ch, PointerEventData pointerEvent)
		{
			if (!IsConditionPass(pointerEvent))
				return;
			InternalMouseUp(ch, pointerEvent);
		}
		protected abstract void InternalMouseUp(GxModelView ch, PointerEventData pointerEvent);
		bool IEquatable<object>.Equals(object other)
		{
			if (other is not GxClickableBase o)
				return false;
			return other.Equals(o);
		}

	}
}