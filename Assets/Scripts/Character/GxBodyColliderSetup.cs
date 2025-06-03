using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
using BoingKit;
using System.Linq;
namespace Gaia
{
    [RequireComponent(typeof(Animator))]
    public class GxBodyColliderSetup : MonoBehaviour
    {
		private static Animator s_Animator;
		[SerializeField] bool m_RunSetup = false;
		[SerializeField] BoingBones m_BoingBones = null;

		private void OnValidate()
		{
			if (Application.isPlaying)
			{
				Debug.LogError("Can only execute in editor mode.");
				return;
			}
			if (m_RunSetup)
			{
				m_RunSetup = false;
				ManuallySetup();
				if (m_BoingBones != null)
				{
					var characterLayer = LayerMask.NameToLayer("Character");
					var colliders = s_Animator
						.GetComponentsInChildren<Collider>()
						.Where(o => o.gameObject.layer == characterLayer)
						.ToArray();

					m_BoingBones.UnityColliders = colliders;
				}
			}
		}

		[ContextMenu("Run Setup")]
		private void ManuallySetup()
        {
			if (s_Animator == null)
				s_Animator = GetComponent<Animator>();
			U3DColliderBoneSetup.ExecuteAutoSetup(s_Animator);
		}
	}
}