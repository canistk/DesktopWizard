using DesktopWizard;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	public struct GxPointerEventData
	{
		public PointerEventData.InputButton button;
		public Vector2 monitorPosition, formPosition, scrollDelta, delta;
		public Vector2Int osPosition;
		public Ray ray;
		public GxPointerEventData(GxModelView win, PointerEventData pointerEvent)
		{
			var o = win.dwCamera;
			var v2i = DwCore.GetOSCursorPos();
			this.osPosition = v2i;
			this.monitorPosition = o.MatrixOSToMonitor().MultiplyPoint3x4(new Vector3(v2i.x, v2i.y, 0f));
			this.formPosition = o.MatrixMonitorToForm().MultiplyPoint3x4(monitorPosition);
			this.ray = o.linkCamera.ScreenPointToRay(formPosition);

			this.button = pointerEvent.button;
			this.delta = pointerEvent.delta;
			this.scrollDelta = pointerEvent.scrollDelta;
		}
	}

	public struct GxDragInfo
	{
		public readonly Vector2 monitorStartPos, formStartPos;
		/// <summary>
		/// Vector in monitor space, which is the offset from the mouse start drag position to the FORM's pivot position.
		/// </summary>
		public Vector2 monitorOffset;
		public Vector2 formOffset;
		public Matrix4x4 o2m, m2o, m2f, f2m;
		public PointerEventData.InputButton inputButton;

		public bool IsActive { get; private set; }
		public bool IsDragging { get; private set; }
		public Vector2 LastPos { get; private set; }
		public Vector2 Delta { get; private set; }

		public GxDragInfo(GxModelView win, PointerEventData pointerEvent)
		{
			this.inputButton = pointerEvent.button;
			this.IsActive = true;
			this.IsDragging = false;
			this.monitorStartPos = 
			this.LastPos = pointerEvent.pointerCurrentRaycast.screenPosition;
			this.Delta = Vector2.zero;

			// U3D's UGUI system using FormSpace coordinate (PointerEventData.position).
			var df = win.dwForm;

			this.o2m	= win.dwCamera.MatrixOSToMonitor();
			this.m2o	= o2m.inverse;
			this.m2f	= win.dwCamera.MatrixMonitorToForm();
			this.f2m	= m2f.inverse;
			var dwFormPosInMon = (Vector2)o2m.MultiplyPoint3x4(new Vector3(df.Left, df.Top, 0));

			// calculate click pos offset based on monitor space
			this.monitorOffset	= dwFormPosInMon - LastPos;
			this.formOffset = m2f.MultiplyPoint3x4(monitorOffset);
			this.formStartPos = m2f.MultiplyPoint3x4(monitorStartPos);
		}
		public void StartDrag(GxModelView win, PointerEventData pointerEvent)
		{
			this.IsDragging = true;
		}

		public void UpdateDrag(GxModelView win, PointerEventData pointerEvent)
		{
			if (pointerEvent.button != inputButton)
				return;
			var monPos = win.dwCamera.GetMousePosInMonitorSpace();
			this.Delta = LastPos - monPos;
			this.LastPos = monPos;
		}

		public void Reset()
		{
			this.IsActive = false;
			this.IsDragging = false;
			this.LastPos = Vector2.zero;
			this.Delta = Vector2.zero;
			this.monitorOffset = Vector2.zero;
			this.o2m = Matrix4x4.identity;
			this.m2o = Matrix4x4.identity;
			this.inputButton = PointerEventData.InputButton.Left;
		}
	}

	public abstract class GxMouseBase : MonoBehaviour
	{
		protected GxModelView win { get; private set; }


		protected virtual void OnEnable()
		{
			win = GetComponentInParent<GxModelView>();
			if (win == null)
				return;
			if (this is not IPointerFeature feature)
			{
				Debug.LogError($"GxMouseBase: {this} is not IPointerFeature, please check the code.");
				return;
			}
			win.Register(feature);
		}

		protected virtual void OnDisable()
		{
			if (win == null)
				return;
			if (this is not IPointerFeature feature)
				return;
			win.Unregister(feature);
			win = null;
		}
	}

	public abstract class GxDraggableBase : GxMouseBase, IPointerDraggableFeature
	{
		[SerializeField] private PointerEventData.InputButton m_Button = PointerEventData.InputButton.Left;
		protected virtual PointerEventData.InputButton GetButtonRef() => m_Button;
		bool IPointerFeature.isActive => gameObject.activeInHierarchy;

		public bool IsHolding => m_DragInfo.IsActive;
		public bool IsDragging => m_DragInfo.IsDragging;

		private GxDragInfo m_DragInfo;

		[System.Obsolete("Use TryGetDragInfo() instead.", true)]
		public GxDragInfo GetDragInfo() => m_DragInfo;

		public bool TryGetDragInfo(out GxDragInfo dragInfo)
		{
			if (m_DragInfo.IsActive)
			{
				dragInfo = m_DragInfo;
				return true;
			}
			dragInfo = default;
			return false;
		}

		protected virtual bool IsConditionPass(PointerEventData pointerEventData)
		{
			return pointerEventData.button == GetButtonRef();
		}

		protected abstract void OnStartDrag(GxModelView win, GxPointerEventData evt);
		protected abstract void OnDragging(GxModelView win, GxPointerEventData evt);
		protected abstract void OnEndDrag(GxModelView win, GxPointerEventData evt);

		void IPointerFeature.MouseDown(GxModelView win, PointerEventData pointerEvent)
		{
			if (!IsConditionPass(pointerEvent))
				return;

			if (m_DragInfo.IsActive || m_DragInfo.IsDragging)
			{
				Debug.LogWarning("Do we had this case? m_Drag.IsActive=" + m_DragInfo.IsActive + " m_Drag.IsDragging=" + m_DragInfo.IsDragging);
				return;
			}

			m_DragInfo = new GxDragInfo(win, pointerEvent);
			// Debug.Log($"Mouse Down, monPos={m_DragInfo.LastPos:F2} IsHolding={IsHolding},IsDragging={IsDragging}");
		}

		/// <summary>
		/// Assume the dragging Form are related to the mouse init position.
		/// calculate the offset in monitor space and convert back to OS space.
		/// </summary>
		/// <param name="win"></param>
		/// <param name="evt"></param>
		/// <returns></returns>
		[System.Obsolete("Use GetDragInfo() instead.", true)]
		protected Vector2Int CalculateFormOffsetInOSSpace(GxModelView win, GxPointerEventData evt)
		{
			//var monPos = win.dwCamera.GetMousePosInMonitorSpace();
			if (!m_DragInfo.IsActive)
				throw new System.Exception("m_Drag.IsActive is false, please check the code.");
			if (!m_DragInfo.IsDragging)
				throw new System.Exception("m_Drag.IsDragging is false, please check the code.");

			var monPos = evt.monitorPosition;
			// var monFromPos = new Vector3(monPos.x + m_DragInfo.monitorOffset.x, monPos.y + m_DragInfo.monitorOffset.y);
			var monFromPos = monPos + m_DragInfo.monitorOffset;
			var r = this.m_DragInfo.m2o.MultiplyPoint3x4(monFromPos);
			var osFromPos = new Vector2Int((int)r.x, (int)r.y);
			return osFromPos;
		}

		void IPointerFeature.MouseMove(GxModelView win, PointerEventData pointerEvent)
		{
			if (!IsHolding)
				return;
			var monPos = win.dwCamera.GetMousePosInMonitorSpace();
			var deltaTest = m_DragInfo.LastPos - monPos;
			if (deltaTest.magnitude < 1f)
				return; // ignore while it smaller than single dpi.
			
			var evt = new GxPointerEventData(win, pointerEvent);
			if (!m_DragInfo.IsDragging)
			{
				m_DragInfo.StartDrag(win, pointerEvent);
				OnStartDrag(win, evt);
			}
			m_DragInfo.UpdateDrag(win, pointerEvent);
			// Debug.Log($"Mouse Move, monPos={m_DragInfo.LastPos:F2}, delta={m_DragInfo.Delta:F2} IsHolding={IsHolding},IsDragging={IsDragging}");

			// Debug.Log($"Mouse Move, diff={diff:F4}, diffScaled:{diffScaled:F4}");
			OnDragging(win, evt);
		}

		void IPointerFeature.MouseUp(GxModelView win, PointerEventData pointerEvent)
		{
			if (pointerEvent.button != GetButtonRef())
				return;

			var earlyExit = !m_DragInfo.IsDragging;
			m_DragInfo.Reset();

			if (earlyExit)
			{
				// Debug.Log($"Mouse Up without drag.");
				return;
			}

			var evt = new GxPointerEventData(win, pointerEvent);
			// Debug.Log($"Mouse Up, monPos={evt.monitorPosition:F2} IsHolding={IsHolding},IsDragging={IsDragging}");

			OnEndDrag(win, evt);
		}

	}

}