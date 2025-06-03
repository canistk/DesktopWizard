using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Gaia
{
	public class TestCtrlBone : MonoBehaviour
	{
		public GxCtrlBone m_Test;

		private void Update()
		{
			if (m_Test == null)
				return;
			m_Test.ApplyCoordinate();
		}

		private void OnDrawGizmos()
		{
			if (m_Test == null)
				return;
			
		}

		[ContextMenu("Cache Offset")]
		private void CacheOffset()
		{
			m_Test.CacheOffset();
		}
	}
}