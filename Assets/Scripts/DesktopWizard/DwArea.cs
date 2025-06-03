using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DesktopWizard
{
	[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
	public abstract class DwArea : MonoBehaviour
	{
		[SerializeField]
		private BoxCollider m_Collider;
		public new BoxCollider collider
		{
			get
			{
				if (m_Collider == null)
					m_Collider = GetComponent<BoxCollider>();
				return m_Collider;
			}
		}
		[SerializeField]
		private Rigidbody m_Rigidbody;
		public new Rigidbody rigidbody
		{
			get
			{
				if (m_Rigidbody == null)
					m_Rigidbody = GetComponent<Rigidbody>();
				return m_Rigidbody;
			}
		}
		public OSRect rect { get; protected set; }

		public void Init(OSRect rect)
		{
			this.rect = rect;
			var o = rect.GetCorners();
			var lb = o[0];
			var lt = o[1];
			var rt = o[2];
			//var rb = o[3];
			var w = Mathf.Abs(lt.x - rt.x);
			var h = Mathf.Abs(lt.y - lb.y);
			collider.size = new Vector2(w, h);
			//collider.offset = new Vector2(w * 0.5f, h * 0.5f);
			transform.position = new Vector3(lb.x + w * 0.5f, lb.y + h * 0.5f, 0f);
		}

		protected virtual void Awake()
		{
			rigidbody.isKinematic = true;
			rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.constraints = RigidbodyConstraints.FreezeAll;

			//rigidbody.WakeUp();
			collider.isTrigger = true;
		}

		public Vector3 Align(Vector3 anchor, DwArea area, Vector3 pivot)
		{
			return CalculateAlignment(collider, anchor, area.collider, pivot);
		}

		/// <summary>
		/// assume outer area's boxcollider are bigger then inner.
		/// </summary>
		/// <param name="outter"></param>
		/// <param name="outterAnchor">The anchor point represent the normalize position within the outter boxcollider scaled by 0~1 percent on each axis.
		/// X-Axis, Left = 0, Right = 1,
		/// Y-Axis, Top = 0, Bottom = 1,
		/// </param>
		/// <param name="inner"></param>
		/// <param name="innerPivot">The inner boxcollider's normalize position within the inner boxcollider.</param>
		/// <returns>the recommend position for inner area.</returns>
		public static Vector3 CalculateAlignment(BoxCollider outter, Vector3 outterAnchor, BoxCollider inner, Vector3 innerPivot)
		{
			var p0 = outter.transform.position;
			var s0 = outter.size * 0.5f;
			var omin = p0 + outter.center - s0;
			var omax = p0 + outter.center + s0;
			var alignmentPoint = new Vector3(
				Mathf.LerpUnclamped(omin.x, omax.x, outterAnchor.x),
				Mathf.LerpUnclamped(omin.y, omax.y, outterAnchor.y),
				Mathf.LerpUnclamped(omin.z, omax.z, outterAnchor.z)
			);

			/****
			var arr = BoundsExtend.GetVertices(outter.bounds);
			foreach (var v in arr)
			{
				DebugExtend.DrawPoint(v, Color.green, 10f, 1f, false);
			}
			DebugExtend.DrawPoint(alignmentPoint, Color.red, 10f, 1f, false);
			//****/

			// innerPivot remapping
			// Notes:
			// Viewport bottom left is (0, 0), top right is (1, 1)
			// BoxCollider center is in local space, min = center - size, max = center + size
			// e.g Cube size = 1, center = (0, 0, 0), min = (-0.5, -0.5, -0.5), max = (0.5, 0.5, 0.5)
			//

			var w = inner.size.x;
			var h = inner.size.y;
			// var z = inner.size.z;
			var pivotPoint = new Vector3(
				Mathf.LerpUnclamped(0f, w, innerPivot.x),
				Mathf.LerpUnclamped(-h, 0f, innerPivot.y),
				0f //Mathf.LerpUnclamped(imin.z, imax.z, innerPivot.z)
			);
			var rst = alignmentPoint - pivotPoint;
			return rst;
		}

		public static Vector3 CalcInnerPointNormalize(BoxCollider c, Vector3 p)
		{
			var b = c.bounds;
			var p1 = c.ClosestPoint(p);
			return new Vector3(
				Mathf.InverseLerp(b.min.x, b.max.x, p1.x),
				Mathf.InverseLerp(b.min.y, b.max.y, p1.y),
				Mathf.InverseLerp(b.min.z, b.max.z, p1.z)
			);
		}

		public void GetCorner(
			out Vector3 leftBottom,
			out Vector3 leftTop,
			out Vector3 rightTop,
			out Vector3 rightBottom
			)
		{
			var o = GetCorner();
			leftBottom	= o[0];
			leftTop		= o[1];
			rightTop	= o[2];
			rightBottom	= o[3];
		}

		/// <summary>
		/// Get the four corners of this area. in monitor space.
		/// </summary>
		/// <returns></returns>
		public Vector3[] GetCorner()
		{
			var p0 = transform.position;
			var s0 = collider.size * 0.5f;
			var omin = p0 + collider.center - s0;
			var omax = p0 + collider.center + s0;
			var leftBottom	= new Vector3(omin.x, omin.y, 0f);
			var leftTop		= new Vector3(omin.x, omax.y, 0f);
			var rightBottom	= new Vector3(omax.x, omin.y, 0f);
			var rightTop	= new Vector3(omax.x, omax.y, 0f);
			return new Vector3[] {
				leftBottom,
				leftTop,
				rightTop,
				rightBottom,
			};
		}

		/// <summary>
		/// Get the bounds of this area. in monitor space.
		/// </summary>
		/// <returns></returns>
		public Bounds GetMonitorBounds()
		{
			var corner = GetCorner();
			var rst = new Bounds(corner[0], Vector3.zero);
			rst.Encapsulate(corner[1]);
			rst.Encapsulate(corner[2]);
			rst.Encapsulate(corner[3]);
			return rst;
		}

		public void DebugDraw(Color? color, float duration = 0f, bool depthTest = false)
		{
			var c = color.HasValue ? color.Value : Color.white;
			GetCorner(out var leftBottom, out var leftTop, out var rightTop, out var rightBottom);
			//const float size = 40f;
			//DebugExtend.DrawPoint(leftBottom, Color.red, size);
			//DebugExtend.DrawPoint(rightTop, Color.blue, size);
			//DebugExtend.DrawPoint(leftTop, Color.green, size);
			//DebugExtend.DrawPoint(rightBottom, Color.cyan, size);

			DebugExtend.DrawLine(leftBottom,	leftTop,	c, duration, depthTest);
			DebugExtend.DrawLine(leftTop,		rightTop,	c, duration, depthTest);
			DebugExtend.DrawLine(rightTop,		rightBottom,c, duration, depthTest);
			DebugExtend.DrawLine(rightBottom,	leftBottom,	c, duration, depthTest);
		}
	}
}