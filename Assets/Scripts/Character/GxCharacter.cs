using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2.Task;
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

        [SerializeField] GxRetargeting m_Retargeting;
        public GxRetargeting Retargeting => m_Retargeting;
        
		public void Blend(GxTimelineAsset timeline, TargetInfo targetInfo)
        {
            Retargeting.AddTarget(targetInfo);
		}

        private List<MyTaskBase> m_Tasks = new List<MyTaskBase>();

		private void Update()
		{
			MyTaskHandler.ManualTasksUpdate(m_Tasks);
		}
	}
}