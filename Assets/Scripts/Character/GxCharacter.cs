using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxCharacter : MonoBehaviour
    {
        [SerializeField] BodyLayout BodyLayout;

        private bool m_FetchBodyLayout = false;
		public Animator animator
        {
            get
            {
                if (BodyLayout == null && !m_FetchBodyLayout)
                {
                    BodyLayout = GetComponentInChildren<BodyLayout>();
                    m_FetchBodyLayout = true;
                    if (BodyLayout == null)
                        throw new System.NullReferenceException("GxCharacter requires a BodyLayout component in its children.");
                    if (BodyLayout.animator == null)
                        throw new System.NullReferenceException("BodyLayout requires an Animator component.");
				}
                return BodyLayout.animator;
            }
		}
	}
}