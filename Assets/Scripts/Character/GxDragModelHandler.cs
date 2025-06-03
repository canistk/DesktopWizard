using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxDragModelHandler : GxDraggableBase
	{
		[SerializeField] Transform m_Target;
		
		[SerializeField] bool m_FlipX = false;
		[SerializeField] bool m_FlipY = false;


		public Transform target { get; private set; } = null;
		Quaternion m_Rotation;

		private void HandleTarget()
		{
			if (target != m_Target)
			{
				target = m_Target;
				// snap to current model rotation.
				m_Rotation = target ? target.rotation : Quaternion.identity;
			}

			if (target == null)
				return;

			target.rotation = m_Rotation;
		}

		private void Update()
		{
			HandleTarget();
		}

		protected override void OnStartDrag(GxModelView win, GxPointerEventData evt)
		{
		}
		protected override void OnDragging(GxModelView win, GxPointerEventData evt)
		{
			var _x = m_FlipX ? evt.delta.x : -evt.delta.x;
			var _y = m_FlipY ? evt.delta.y : -evt.delta.y;

			var rotY = Quaternion.AngleAxis(_x, target.up);
			var rotX = Quaternion.AngleAxis(_y, Vector3.right);
			m_Rotation = rotX * rotY * m_Rotation;
		}
		protected override void OnEndDrag(GxModelView win, GxPointerEventData evt)
		{
		}
    }
}