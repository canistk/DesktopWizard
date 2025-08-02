//#define HIDE_VRMA_PREFAB
using Kit2;
using Kit2.ObjectPool;
using Kit2.Tasks;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

		public async void CrossFade(GxMotionKey key, float fadeIn, System.Action<GxMotionTask> taskCreated = null)
		{
			var src = key.Type == eAssetType.VRMA ?
				eSrcType.GameObject :
#if USE_ADDRESSABLE
				eSrcType.Addressable;
#else
				eSrcType.Resources;
#endif

			if (animator.enabled)
			{
				Debug.LogWarning("Animator should be closed.", this);
				animator.enabled = false;
			}
			var handler = await GxMotionPool.Instance.GetHandler(key, transform, fadeIn);
			var task = new GxMotionTask(this, handler, fadeIn);
			if (taskCreated != null)
				taskCreated.TryCatchDispatchEventError(o => o?.Invoke(task));
			m_Tasks.Add(task);
		}

		public void ChangePose(string poseKey, float fadeIn, System.Action<GxPoseTask> taskCreated = null)
		{
			var task = new GxPoseTask(poseKey, this);
			if (taskCreated != null)
				taskCreated.TryCatchDispatchEventError(o => o?.Invoke(task));
			m_Tasks.Add(task);
		}

		public void RandomPose(float fadeIn, System.Action<GxPoseTask> taskCreated = null)
		{
			if (!GxMotionDatabase.TryGetRandomPoseKey(out var pose))
			{
				Debug.LogError("No pose keys available in GxMotionDatabase.");
				return;
			}

			if (pose == null)
			{
				Debug.LogError("No pose keys available in GxMotionDatabase.");
				return;
			}
			ChangePose(pose.key, fadeIn, taskCreated);
		}

		public event System.Action<IRetarget> EVENT_WillPlayMotion;

		/// <summary>Called by <see cref="GxTimelineTask"/></summary>
		/// <param name="ani"></param>
		internal void BoardcastWillPlayAnimation(IRetarget ani)
        {
			//int i = m_Tasks.Count;
			//while (i-- > 0)
			for (int i =0; i < m_Tasks.Count; ++i)
			{
				if (m_Tasks[i] == ani)
					continue;
				if (m_Tasks[i] is not IRetarget aniTask)
					continue;
				if (aniTask == ani)
					continue;
				aniTask.OnWillPlayAnimation(ani);
			}
			EVENT_WillPlayMotion?.TryCatchDispatchEventError(o => o?.Invoke(ani));
        }

		/// <summary>Called by <see cref="GxTimelineTask"/></summary>
		internal void BoardCastPlayedOnce(IRetarget ani)
        {
            
		}

		public bool TrySearchForAnimationTask(GxMotionKey key, out GxMotionTask motionTask)
		{
			motionTask = default;
			if (m_Retargeting == null)
			{
				Debug.LogError("GxRetargeting is not initialized.");
				return false;
			}
			foreach (var task in m_Tasks)
			{
				if (task is not GxMotionTask mt)
					continue;
				if (!mt.Key.Equals(key))
					continue;
				motionTask = mt;
				return true;
			}

			return false;
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

		public void CleanTask()
		{
			foreach (var task in m_Tasks)
			{
				if (task is MyTask t && !t.isCompleted)
				{
					t.Abort(); // Abort the task if it's not completed
				}
			}
			m_Tasks.Clear();
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