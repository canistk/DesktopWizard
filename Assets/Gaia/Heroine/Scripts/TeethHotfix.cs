using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
namespace Gaia
{
    public class TeethHotfix : MonoBehaviour, IBlendShapeRequest
	{
		public FaceRig m_FaceRig = null;
		private void OnEnable()
		{

			if (m_FaceRig == null)
			{
				m_FaceRig = GetComponentInChildren<FaceRig>(true);
				if (m_FaceRig == null)
				{
					Debug.LogError($"Fail to locate {nameof(FaceRig)}, critical fail, feature disable.");
					this.enabled = false;
					return;
				}
			}
			m_FaceRig.Register(this);
		}

		private void OnDisable()
		{
			if (m_FaceRig)
				m_FaceRig.Unregister(this);
			
		}

		#region Face Rig
		public float GetBlendWeight()
		{
			return this.enabled ? 0.1f : 0f;
		}

		public IEnumerable<BlendShapeRequest> GetBlendShapeRequests()
		{
			yield return new BlendShapeRequest((int)eHeroineFaceRig.up_tooth_hide, 1f);
			yield return new BlendShapeRequest((int)eHeroineFaceRig.down_tooth_hide, 1f);
		}
		#endregion Face Rig;
	}
}