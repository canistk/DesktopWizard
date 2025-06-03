using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	/// <summary>
	/// A class for dragging the form.
	/// </summary>
	public class GxDragFormHandler : GxDraggableForm
	{
		protected override void OnStartDrag(GxModelView win, GxPointerEventData evt)
		{
		}
		protected override void OnDragging(GxModelView win, GxPointerEventData evt)
		{
			if (!TryGetDragInfo(out var info))
				return;
			var monPos = evt.monitorPosition + info.monitorOffset;
			if (win.dwWindow != null)
			{
				win.dwWindow.MoveTo_Monitor(monPos);
				return;
			}

			var dragPos = info.m2o.MultiplyPoint3x4(monPos);
			win.dwForm.MoveTo_OS((int)dragPos.x, (int)dragPos.y);
		}

		protected override void OnEndDrag(GxModelView win, GxPointerEventData evt)
		{
		}
	}
}