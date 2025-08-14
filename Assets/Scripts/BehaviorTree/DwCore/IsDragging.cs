using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Is Window Dragging")]
	[TaskDescription("Check if Window dragging.")]

	public class IsDragging : WinConditional
	{
		[Header("Processing Data")]
		[RequiredField]
		public SharedDragData m_DragInfo;

		private DragData dragInfo
		{
			get
			{
				if (m_DragInfo.Value == null)
				{
					Debug.LogWarning("Non init drag info detected.");
					m_DragInfo.SetValue(new DragData());
				}
				return m_DragInfo.Value;
			}
		}
		protected override eState OnModelViewUpdate()
		{
			return dragInfo.IsDragging ?
				eState.Success :
				eState.Failure;
		}
	}
}