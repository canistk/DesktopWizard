using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class EyeBrowCtrl : MonoBehaviour, IBlendShapeRequest
	{
		private void InternalEyeBrowCtrl() 
		{
			Set(eHeroineFaceRig.browDownLeft,		0f);
			Set(eHeroineFaceRig.browDownRight,		0f);
			Set(eHeroineFaceRig.browInnerUp,		0f);
			Set(eHeroineFaceRig.browOuterUpLeft,	0f);
			Set(eHeroineFaceRig.browOuterUpRight,	0f);
		}

		#region IFaceRigCtrl
		private Dictionary<eHeroineFaceRig, float> m_Cache = new Dictionary<eHeroineFaceRig, float>();
		//private void Set(eFaceRig rig, float weight) => helper.SetBlendShapeWeight((int)rig, weight);
		private void Set(eHeroineFaceRig rig, float weight) => m_Cache[rig] = weight;
		IEnumerable<BlendShapeRequest> IBlendShapeRequest.GetBlendShapeRequests()
		{
			foreach ((var rig, var weight) in m_Cache)
			{
				yield return new BlendShapeRequest((int)rig, weight);
			}
		}

		float IBlendShapeRequest.GetBlendWeight() => enabled ? 1f : 0f;
		#endregion IFaceRigCtrl
	}
}