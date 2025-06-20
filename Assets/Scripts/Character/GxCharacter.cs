using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2.Tasks;
using Kit2.ObjectPool;
using Kit2;
namespace Gaia
{
    public class GxCharacter : MonoBehaviour
    {
        [SerializeField] BodyLayout m_BodyLayout;

        private bool m_FetchBodyLayout = false;
		public Animator animator
        {
            get
            {
                if (m_BodyLayout == null && !m_FetchBodyLayout)
                {
                    m_BodyLayout = GetComponentInChildren<BodyLayout>();
                    m_FetchBodyLayout = true;
                    if (m_BodyLayout == null)
                        throw new System.NullReferenceException("GxCharacter requires a BodyLayout component in its children.");
                    if (m_BodyLayout.animator == null)
                        throw new System.NullReferenceException("BodyLayout requires an Animator component.");
				}
                return m_BodyLayout.animator;
            }
		}

        [SerializeField] GxRetargeting m_Retargeting;
        public GxRetargeting Retargeting => m_Retargeting;
        
        [SerializeField] kObjectPool m_Pool;

		private List<MyTaskBase> m_Tasks = new List<MyTaskBase>();

		private void Reset()
		{
			m_BodyLayout = GetComponentInChildren<BodyLayout>();
            m_Retargeting = GetComponentInChildren<GxRetargeting>();
            m_Pool = transform.GetOrAddComponent<kObjectPool>();
		}

		private void Update()
		{
			MyTaskHandler.ManualParallelUpdate(m_Tasks);
		}

        public void CrossFade(string timelineAssetPath, float fadeIn)
        {
            if (string.IsNullOrEmpty(timelineAssetPath))
                throw new System.ArgumentNullException(nameof(timelineAssetPath), "Timeline asset path cannot be null or empty.");
            var timelineAssetGo = m_Pool.Spawn(timelineAssetPath, true, transform, false);
            if (timelineAssetGo == null)
            {
                Debug.LogError($"Failed to spawn timeline asset from path: {timelineAssetPath}");
                return;
			}
            var timelineAsset = timelineAssetGo.GetComponent<GxTimelineAsset>();
			var aniTask = new GxAnimationTask(this, timelineAsset , fadeIn);
            m_Tasks.Add(aniTask);
		}

        public IEnumerator<GxAnimationTask> GetActiveAnimations()
        {
            foreach (var task in m_Tasks)
            {
                if (task is not GxAnimationTask aniTask)
                    continue;
                if (aniTask.isCompleted)
                    continue;
                yield return aniTask;
			}
		}
	}
} 