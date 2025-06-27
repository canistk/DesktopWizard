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
        
		private void Reset()
		{
            Editor_RetargetingCreate();
			Editor_ObjectPoolCreate();
		}

		public void RuntimeCreation()
		{
			Editor_RetargetingCreate();
			if (m_Retargeting != null)
			{
				m_Retargeting.ForceTPose();
			}
			Editor_ObjectPoolCreate();
			#if DEBUG
			_ = transform.GetOrAddComponent<GxTimelineHelper>();
			#endif
		}

		private void Update()
		{
            HandleTasks();
		}

		#region Task Management
		private List<MyTaskBase> m_Tasks = new List<MyTaskBase>();
        private void HandleTasks()
        {
			MyTaskHandler.ManualParallelUpdate(m_Tasks);
		}
		#endregion Task Management

		#region Object Pooling
		[SerializeField] kObjectPool m_Pool;
        private void Editor_ObjectPoolCreate()
        {
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
		#endregion Object Pooling

		#region Wrapped Retargeting Methods
		[SerializeField] GxRetargeting m_Retargeting;
		private bool m_Fetched = false;
		public Animator animator
		{
			get
			{
				if (m_Retargeting == null && !m_Fetched)
				{
					m_Retargeting = GetComponentInChildren<GxRetargeting>();
					m_Fetched = true;
					if (m_Retargeting == null)
						throw new System.NullReferenceException("GxCharacter requires a Retargeting component in its children.");
					if (m_Retargeting.animator == null)
						throw new System.NullReferenceException("Retargeting requires an Animator component.");
				}
				return m_Retargeting.animator;
			}
		}

		private void Editor_RetargetingCreate()
        {
			m_Retargeting = GetComponentInChildren<GxRetargeting>();
			if (m_Retargeting == null)
			{
				var animator = GetComponentInChildren<Animator>();
				if (animator == null)
				{
					Debug.LogError($"Unable to create {nameof(GxRetargeting)} required component animator.");
				}
				m_Retargeting = animator.gameObject.AddComponent<GxRetargeting>();
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
				if (!m_Retargeting.IsLateUpdate)
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

        public void AddAnimationRetarget(IRetarget target)
        {
            if (m_Retargeting == null)
            {
                Debug.LogError("GxRetargeting is not initialized.");
                return;
            }
            m_Retargeting.AddTarget(target);
		}

        public void RemoveAnimationRetarget(IRetarget target)
        {
            if (m_Retargeting == null)
            {
                Debug.LogError("GxRetargeting is not initialized.");
                return;
            }
            m_Retargeting.RemoveTarget(target);
		}
		#endregion Wrapped Retargeting Methods

		#region Face Rig
		private KeyValuePair<bool, FaceRig> m_FaceRig;
		public FaceRig FaceRig
		{
			get
			{
				if (!m_FaceRig.Key)
				{
					m_FaceRig = new KeyValuePair<bool, FaceRig>(true, GetComponentInChildren<FaceRig>(true));
					Debug.Assert(m_FaceRig.Value != null, "FaceRig component is missing in the children of GxCharacter.");
				}
				return m_FaceRig.Value;
			}
		}

		#endregion Face Rig

		#region Emotion Wheel
		private KeyValuePair<bool, EmotionWheel> m_EmotionWheel;
		public EmotionWheel EmotionWheel
		{
			get
			{
				if (!m_EmotionWheel.Key)
				{
					m_EmotionWheel = new KeyValuePair<bool, EmotionWheel>(true, GetComponentInChildren<EmotionWheel>(true));
					Debug.Assert(m_EmotionWheel.Value != null, "EmotionWheel component is missing in the children of GxCharacter.");
				}
				return m_EmotionWheel.Value;
			}
		}
		#endregion Emotion Wheel
	}
} 