using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class PointsDistance : MonoBehaviour
	{
		public Transform pointA;

		public Transform pointB;

		public Color m_Color = Color.red;

		private void OnDrawGizmos()
		{
			if (pointA == null || pointB == null)
				return;

			using (new ColorScope(m_Color))
			{
				Gizmos.DrawLine(pointA.position, pointB.position);

				float distance = Vector3.Distance(pointA.position, pointB.position);
				var center = Vector3.Lerp(pointA.position, pointB.position, 0.5f);
				GizmosExtend.DrawLabel(center, $"{distance:F4}");
			}
		}
	}
}