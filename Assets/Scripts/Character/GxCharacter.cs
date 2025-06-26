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
            // m_Pool = transform.GetOrAddComponent<kObjectPool>();
            m_Pool = GetComponentInChildren<kObjectPool>();
            if (m_Pool == null)
            {
                var tran = new GameObject("Loader").transform;
                tran.SetParent(transform, false);
                tran.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                m_Pool = tran.gameObject.AddComponent<kObjectPool>();
            }
		}

		private void Update()
		{
			MyTaskHandler.ManualParallelUpdate(m_Tasks);
		}

        private void InternalPlayTimeline(GxTimelineAsset timelineAsset, float fadeIn, bool realTime)
        {
            if (timelineAsset == null)
                throw new System.ArgumentNullException(nameof(timelineAsset), "Timeline asset cannot be null.");
            var aniTask = new GxAnimationTask(this, timelineAsset, fadeIn, realTime);

			// Hack : while retargeting system is using Update (prefer to use LateUpdate instead),
            // we need to disable animator in Update to prevent animation flickering/overrided.
			if (animator.enabled)
            {
                Debug.LogWarning("Retargeting is enabled in Update, disabling it to prevent flickering. Consider using LateUpdate for retargeting.");
				if (!Retargeting.IsLateUpdate)
                    animator.enabled = false;
            }

            m_Tasks.Add(aniTask);
        }

		public void CrossFade(string timelineAssetPath, float fadeIn, bool realTime = false)
        {
			if (string.IsNullOrEmpty(timelineAssetPath))
                throw new System.ArgumentNullException(nameof(timelineAssetPath), "Timeline asset path cannot be null or empty.");
            var timelineAssetGo = m_Pool.Spawn(timelineAssetPath, true, null, false);
            if (timelineAssetGo == null)
            {
                Debug.LogError($"Failed to spawn timeline asset from path: {timelineAssetPath}");
                return;
			}
            var timelineAsset = timelineAssetGo.GetComponent<GxTimelineAsset>();
            if (timelineAsset == null)
            {
                Debug.LogError($"The spawned GameObject does not have a GxTimelineAsset component: {timelineAssetGo.name}");
                return;
            }
            InternalPlayTimeline(timelineAsset, fadeIn, realTime);
		}

        /// <summary>Called by <see cref="GxAnimationTask"/></summary>
        /// <param name="ani"></param>
        internal void BoardcastWillPlayAnimation(GxAnimationTask ani)
        {
            foreach (var at in GetActiveAnimations())
            {
                if (at == ani)
                    continue;
                at.OnWillPlayAnimation(ani);
			}
        }

		/// <summary>Called by <see cref="GxAnimationTask"/></summary>
		internal void BoardCastPlayedOnce(GxAnimationTask ani)
        {
            
		}

        public IEnumerable<GxAnimationTask> GetActiveAnimations()
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