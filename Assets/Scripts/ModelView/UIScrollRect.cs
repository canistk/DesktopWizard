using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gaia
{
	public class UIScrollRect : ScrollRect
	{
		[SerializeField] bool _disableMouseScrolling;

		public RectTransform viewMask;
		public float focusSmoothTime = .1f;
		public bool m_enableScrolling = true;


		float? m_horizontalPos = null;
		float? m_verticalPos = null;

		Vector3? m_focusDst;
		Vector3 m_focusVel;

		public event Action<PointerEventData> OnMouseScroll;

		public Vector3 ViewportCenterPos() { return viewport != null ? viewport.TransformPoint(viewport.rect.center) : Vector3.zero; }
		public bool IsFocusing() { return m_focusDst != null; }

		public Vector3 ViewportPosition(Vector2 offset)
		{
			if (viewport == null)
				return Vector3.zero;

			var r = viewport.rect;
			var o = new Vector2(r.x + r.width * offset.x, r.y + r.height * offset.y);
			var p = viewport.TransformPoint(o);

			return p;
		}

		protected override void Awake()
		{
			base.Awake();
			onValueChanged.AddListener(OnScrollChanged);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			onValueChanged.RemoveListener(OnScrollChanged);
		}

		protected virtual void Update()
		{
			if (m_horizontalPos.HasValue) { horizontalNormalizedPosition = m_horizontalPos.Value; m_horizontalPos = null; }
			if (m_verticalPos.HasValue) { verticalNormalizedPosition = m_verticalPos.Value; m_verticalPos = null; }
		}


		protected override void LateUpdate()
		{
			base.LateUpdate();
			UpdateFocusMovement(Time.unscaledDeltaTime);
		}

		public void SetHorizontalNormalizedPosition(float v) { if (gameObject.activeInHierarchy) { horizontalNormalizedPosition = v; } else { m_horizontalPos = v; } }
		public void SetVerticalNormalizedPosition(float v) { if (gameObject.activeInHierarchy) { verticalNormalizedPosition = v; } else { m_verticalPos = v; } }

		public RectTransform GetViewRect()
		{
			return viewMask == null ? viewRect : viewMask;
		}

		protected virtual void OnScrollChanged(Vector2 pos) { }

		protected override void SetNormalizedPosition(float value, int axis)
		{
			base.SetNormalizedPosition(value, axis);
			m_focusDst = null;
		}

		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (!m_enableScrolling)
				return;
			base.OnBeginDrag(eventData);
			m_focusDst = null;
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (!m_enableScrolling)
				return;
			base.OnDrag(eventData);
			m_focusDst = null;
		}

		public override void OnScroll(PointerEventData eventData)
		{
			OnMouseScroll?.Invoke(eventData);
			if (!m_enableScrolling || _disableMouseScrolling)
				return;
			base.OnScroll(eventData);
			m_focusDst = null;
		}

		// https://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection
		private static bool _Intersect(in Vector2 center, float radius, in Rect rect)
		{
			var d = new Vector2()
			{
				x = Mathf.Abs(center.x - rect.center.x),
				y = Mathf.Abs(center.y - rect.center.y),
			};

			if (d.x > rect.width * 0.5f + radius) return false;
			if (d.y > rect.height * 0.5f + radius) return false;

			if (d.x <= rect.width * 0.5f) return true;
			if (d.y <= rect.height * 0.5f) return true;

			var o2 = Mathf.Pow(d.x - rect.width * 0.5f, 2) +
					 Mathf.Pow(d.y - rect.height * 0.5f, 2);

			return o2 <= Mathf.Pow(radius, 2);
		}
		public bool WithinViewport(Vector3 pos, float radius)
		{
			var localPos = viewport.transform.InverseTransformPoint(pos);
			return _Intersect(localPos, radius, viewport.rect);
		}

		// top - left coordinates
		public Vector3 Focus(RectTransform focus, Vector2 viewportOffset, Vector2 rectOffset)
		{
			if (focus == null) return Vector3.zero;
			return Focus(_RectWS(focus), viewportOffset, rectOffset);

			Rect _RectWS(RectTransform t)
			{
				var c = t.TransformPoint(t.rect.min);
				var s = t.TransformVector(t.rect.size);
				return new Rect(c, s);
			}
		}

		public Vector3 Focus(Rect rect, Vector2 viewportOffset, Vector2 rectOffset)
		{
			rectOffset.y = 1.0f - rectOffset.y;
			var pos = rect.position + rectOffset * rect.size;
			return Focus(pos, viewportOffset);
		}

		public Vector3 Focus(Vector3 pos, Vector2 viewportOffset)
		{
			viewportOffset.y = 1.0f - viewportOffset.y;
			var p0 = pos;
			var p1 = ViewportPosition(viewportOffset);
			var o = p1 - p0;

			if (!horizontal) o.x = 0.0f;
			if (!vertical) o.y = 0.0f;
			o.z = 0.0f;

			m_focusDst = ClampToViewport(content.position + o);
			return m_focusDst.Value;
		}

		public void Unfocus()
		{
			m_focusDst = null;
		}

		public void EnableScrolling(bool v)
		{
			m_enableScrolling = v;
		}

		void UpdateFocusMovement(float dt)
		{
			if (m_focusDst == null)
				return;

			var c = content;
			var d = (c.position - m_focusDst.Value);

			if (d.sqrMagnitude < 0.001f)
			{
				c.position = m_focusDst.Value;
				m_focusDst = null;
				return;
			}

			c.position = _SmoothDamp(c.position, m_focusDst.Value, ref m_focusVel, focusSmoothTime, Mathf.Infinity, dt);

			Vector3 _SmoothDamp(Vector3 src, Vector3 dst, ref Vector3 vel, float smoothTime, float maxSpeed, float dt)
			{
				if (Vector3.SqrMagnitude(src - dst) < Vector3.kEpsilon) return dst;
				return Vector3.SmoothDamp(src, dst, ref vel, smoothTime, maxSpeed, dt);
			}
		}

		Vector3 ClampToViewport(Vector3 dst)
		{
			if (movementType == MovementType.Unrestricted)
				return dst;

			var vsize = viewport.TransformVector(viewport.rect.size);
			var csize = content.TransformVector(content.rect.size);

			var dif = csize - vsize;
			var p0 = viewport.position;
			var p1 = p0 - new Vector3(dif.x, -dif.y);

			var min = new Vector2(p1.x, p0.y);
			var max = new Vector2(p0.x, p1.y);

			dst.x = Mathf.Clamp(dst.x, min.x, max.x);
			dst.y = Mathf.Clamp(dst.y, min.y, max.y);

			return dst;
		}
	}
}