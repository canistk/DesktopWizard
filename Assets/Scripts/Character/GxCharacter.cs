//#define HIDE_VRMA_PREFAB
using Kit2;
using Kit2.ObjectPool;
using Kit2.Tasks;
using System.Collections;
using System.Collections.Generic;
using UniGLTF;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
    public class GxCharacter : MonoBehaviour
    {
        
		private void Reset()
		{
            Editor_RetargetingCreate();
		}

		private void OnDestroy()
		{
		}
		public void RuntimeCreation()
		{
			Editor_RetargetingCreate();
			if (m_Retargeting != null)
			{
				m_Retargeting.ForceTPose();
			}
			#if DEBUG
			_ = transform.GetOrAddComponent<GxMotionHelper>();
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
		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;

		public async void CrossFade(GxMotionKey key, float fadeIn)
		{
			var src = key.Type == eAssetType.VRMA ?
				eSrcType.GameObject :
#if USE_ADDRESSABLE
				eSrcType.Addressable;
#else
				eSrcType.Resources;
#endif
			var handler = await GxMotionPool.Instance.GetHandler(key, transform, fadeIn);
			var task = new GxMotionTask(this, handler, fadeIn);
			m_Tasks.Add(task);
		}

		/// <summary>Called by <see cref="GxTimelineTask"/></summary>
		/// <param name="ani"></param>
		internal void BoardcastWillPlayAnimation(IRetarget ani)
        {
            foreach (var at in GetActiveAnimations())
            {
                if (at == ani)
                    continue;
                at.OnWillPlayAnimation(ani);
			}
        }

		/// <summary>Called by <see cref="GxTimelineTask"/></summary>
		internal void BoardCastPlayedOnce(IRetarget ani)
        {
            
		}

        public IEnumerable<IRetarget> GetActiveAnimations()
        {
            foreach (var task in m_Tasks)
            {
                if (task is not IRetarget aniTask)
                    continue;
                if (task is MyTask t && t.isCompleted)
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

		/*
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
		*/
	}
} 