using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[TaskCategory("DwCore")]
	[TaskName("Set Window Opacity")]
	[TaskDescription("Set Window Opacity.")]
	public class SetWinOpacity : WinAction, IPriorityObj
	{
		[SerializeField] SharedFloat m_Opacity = 1.0f;
		[SerializeField] SharedFloat m_Priority = 1.0f;

		public float Priority => m_Priority.Value;

		public object Value => m_Opacity.Value;

		public int CompareTo(IPriorityObj other) => Priority.CompareTo(other.Priority);

		public bool Equals(IPriorityObj other) => this == (object)other;

		protected override eState OnModelViewUpdate()
		{
			if (ModelView == null || ModelView.dwCamera == null)
				return eState.Failure;

			return eState.Success;
		}

		public override void OnStart()
		{
			base.OnStart();
			if (ModelView != null && ModelView.dwCamera != null)
			{
				ModelView.dwCamera.AddOpacityModifier(this);
			}
		}

		public override void OnEnd()
		{
			base.OnEnd();
			if (ModelView != null && ModelView.dwCamera != null)
			{
				ModelView.dwCamera.RemoveOpacityModifier(this);
			}
		}

		public override void OnReset()
		{
			base.OnReset();

		}
	}
}
