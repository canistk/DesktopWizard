using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	/// <summary>
	/// A test class for dragging ray.
	/// and preform Physics.Raycast based on the drag ray.
	/// the hitted ray will be drawn in Cyan in monitor space.
	/// and the same ray will be drawn in Yellow in model space.
	/// </summary>
	public class GxDragRayTestHandler : GxDraggableForm
	{
		[SerializeField] Animator m_Animator;
		
		[SerializeField] Color m_Color = Color.cyan;
		[SerializeField, Min(0f)] float m_ExtraDistance = 0f;

		Vector3 m_StartPos = Vector3.zero;
		protected override void OnStartDrag(GxModelView win, GxPointerEventData evt)
		{
			m_StartPos = (Vector3)evt.monitorPosition;
			DebugExtend.DrawCircle(m_StartPos, Vector3.forward, m_Color, 10f, 3f, false);
		}

		[SerializeField] Vector2 m_OuterAnchor;
		[SerializeField] Vector2 m_InnerAnchor;
		protected override void OnDragging(GxModelView win, GxPointerEventData evt)
		{
			var toPos = (Vector3)evt.monitorPosition;
			
			var p0 = m_StartPos;
			var p1 = toPos;
			var vector = p1 - p0;
			if (vector == Vector3.zero)
				return;

			var dir = vector.normalized;
			var maxDistance = vector.magnitude + Mathf.Max(0f, m_ExtraDistance);
			if (win.dwCamera.RaycastBorderWin(m_StartPos, dir, maxDistance, out var rst))
			{
				rst.CalcForm(out var fpos, out var fdir, out var fDis, out var fHit);
				rst.CalcMonitor(out var mpos, out var mdir, out var mDis, out var mHit);

				DebugExtend.DrawPoint(mHit, Color.red, 10f, 3f, false);
				DebugExtend.DrawLine(mpos, mHit, m_Color.CloneAlpha(0.2f), 1f, false);

				DebugExtend.DrawLine(fpos, fHit, Color.yellow, 1f, false);
				if (rst.isWindow)
				{
					var info = rst.window.m_WindowInfo;
					Debug.Log($"We found : {info.title}");
					var myWidget = win.dwWindow;

					if (m_Animator != null)
					{
						var bone = m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
						var worldPos = bone.transform.position;
						var scrPos = win.dwCamera.linkCamera.WorldToScreenPoint(worldPos);
						DebugExtend.DrawWireSphere(scrPos, 20f, Color.magenta, 1f, false);
					}

					var anchor = 
					rst.window.Align(m_OuterAnchor, myWidget, m_InnerAnchor);
					DebugExtend.DrawWireSphere(anchor, 20f, Color.cyan, 1f, false);
					
				}
			}
			else
			{
				rst.CalcForm(out var fpos, out var fdir, out var fDis, out var fHit);
				rst.CalcMonitor(out var mpos, out var mdir, out var mDis, out var mHit);

				DebugExtend.DrawLine(mpos, mHit, m_Color.CloneAlpha(0.2f), 1f, false);
				DebugExtend.DrawLine(fpos, fHit, Color.yellow, 1f, false);
			}
		}

		protected override void OnEndDrag(GxModelView win, GxPointerEventData evt)
		{
		}
	}
}