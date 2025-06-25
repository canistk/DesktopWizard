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
        private Queue<GxAnimationTask> m_AnimationQueue = new Queue<GxAnimationTask>();

		private void Reset()
		{
			m_BodyLayout = GetComponentInChildren<BodyLayout>();
            m_Retargeting = GetComponentInChildren<GxRetargeting>();
            m_Pool = transform.GetOrAddComponent<kObjectPool>();
		}

		private void Update()
		{
			MyTaskHandler.ManualParallelUpdate(m_Tasks);
            HandleAnimationFlow();
		}

        private void HandleAnimationFlow()
        {
            Debug.Assert(m_AnimationQueue != null, "Animation queue should not be null.");
            if (m_AnimationQueue.Count == 1)
            {
                var t = m_AnimationQueue.Peek();
                if (!t.isCompleted)
                    t.Execute();
            }
            else if (m_AnimationQueue.Count > 1)
            {
                var t = m_AnimationQueue.Dequeue();
                Debug.Assert(t != null, "Dequeued animation task should not be null.");
                
				if (t != null)
                {
                    InternalPlayTimeline(t, 0f, false);
                }
                else
                {
                    Debug.LogWarning("Received a null timeline asset in the animation queue.");
                }
            }
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

            BoardcastWillPlayAnimation(aniTask);
            m_Tasks.Add(aniTask);
        }

		public void CrossFade(string timelineAssetPath, float fadeIn, bool realTime = false)
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
            if (timelineAsset == null)
            {
                Debug.LogError($"The spawned GameObject does not have a GxTimelineAsset component: {timelineAssetGo.name}");
                return;
            }
            InternalPlayTimeline(timelineAsset, fadeIn, realTime);
		}

        private struct QueuedAnime
        {
            public string timelineAssetPath;
            public float fadeIn;
            public bool realTime;
            public QueuedAnime(string path, float fade, bool realTime)
            {
                timelineAssetPath = path;
                fadeIn = fade;
                this.realTime = realTime;
			}
		}
        Queue<QueuedAnime> m_QueuedAnimes = new Queue<QueuedAnime>();
		public void QueueAnime(string timelineAssetPath, float fadeIn, bool realTime = false)
        {
            Debug.Log($"{timelineAssetPath}, queued.");
			m_QueuedAnimes.Enqueue(new QueuedAnime(timelineAssetPath, fadeIn, realTime));
		}

        public void ClearQueueAnime()
        {
            m_QueuedAnimes.Clear();
		}
        private void BoardcastWillPlayAnimation(GxAnimationTask next)
        {
            foreach (var at in GetActiveAnimations())
            {
                at.OnWillPlayAnimation(next);
			}
        }

        public void BoardCastPlayedOnce(GxAnimationTask anime)
        {
            if (m_QueuedAnimes.Count > 0)
            {
                BoardcastWillPlayAnimation(anime);
                var q = m_QueuedAnimes.Dequeue();
                CrossFade(q.timelineAssetPath, q.fadeIn, q.realTime);
                Debug.Log($"Play queued {q.timelineAssetPath}");
			}
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