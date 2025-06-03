using DesktopWizard;
using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
	public class GxOpacityModifier : MonoBehaviour, IPriorityObj
	{
		[SerializeField] DwCamera dwCamera;
		[SerializeField, Range(0f, 1f)] float m_Priority = 1.0f;
		[SerializeField, Range(0f, 1f)] float m_Value = 1.0f;

		public float Priority => m_Priority;

		public object Value => m_Value;

		public int CompareTo(IPriorityObj other) => Priority.CompareTo(other.Priority);

		public bool Equals(IPriorityObj other) => this == (object)other;

		private void OnValidate()
		{
			if (!Application.isPlaying)
				return;
		}

		private void OnEnable()
		{
			if (dwCamera == null)
				return;
			dwCamera.AddOpacityModifier(this);
		}

		private void OnDisable()
		{
			if (dwCamera == null)
				return;
			dwCamera.RemoveOpacityModifier(this);
		}
	}
}