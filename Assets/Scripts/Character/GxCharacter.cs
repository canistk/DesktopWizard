//#define HIDE_VRMA_PREFAB
using Kit2;
using Kit2.ObjectPool;
using Kit2.Tasks;
using System.Linq;
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
			// _ = transform.GetOrAddComponent<GxMotionHelper>();
#endif

			GenHumanoidColliders();


		}


		private void Update()
		{
            HandleTasks();
			HandleBlinkUpdate();
			HandleExpression();
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

		#region Body Collider
		private void GenHumanoidColliders()
		{
			if (VRM != null)
			{
				m_Tasks.Add(new HotfixVRMControlRigIssue(this, VRM, InternalGenerateCollider));
			}
			else
			{
				InternalGenerateCollider();
			}
		}

		private void InternalGenerateCollider()
		{
			if (animator != null)
				U3DColliderBoneSetup.ExecuteAutoSetup(animator, 1f);
		}

		/// <summary>
		/// VRM 10 control rig being create after VRM10 setup completed.
		/// wait until VRM10 finished.
		/// </summary>
		private class HotfixVRMControlRigIssue : MyTaskWithState
		{
			private readonly GxCharacter character;
			private readonly Vrm10Instance vrm10;
			private float m_StartTime;
			private const float TIMEOUT = 10f;
			private System.Action m_Callback;
			public HotfixVRMControlRigIssue(GxCharacter ch, Vrm10Instance vrm10, System.Action callback)
			{
				this.character = ch;
				this.vrm10 = vrm10;
				this.m_Callback = callback;
			}
			protected override void OnEnter()
			{
				// Question, timescale == 0f;
				m_StartTime = Time.timeSinceLevelLoad;
			}

			protected override bool ContinueOnNextCycle()
			{
				if (character.animator == null)
					throw new System.NullReferenceException();

				if (character.animator.avatar == null)
					return true;
				var avatar = character.animator.avatar;
				if (avatar == null)
					return true;

				//var str = avatar.name;
				//if (str.Contains("runtime control rig", System.StringComparison.OrdinalIgnoreCase))
				if (vrm10.Runtime != null)
				{
					m_Found = true;
					if (m_Callback != null)
					{
						m_Callback.TryCatchDispatchEventError(o => o?.Invoke());
					}
					Debug.Log("Found VRM10 control rig.");
					return false;
				}

				var pass = Time.timeSinceLevelLoad - m_StartTime;
				return pass <= TIMEOUT;
			}

			private bool m_Found;

			protected override void OnComplete()
			{
				if (!m_Found)
					Debug.LogError("Fail to locate VRM10 Control rig");
			}

		}
		#endregion Body Collider

		private KeyValuePair<bool, Vrm10Instance> m_Vrm = default;
		private Vrm10Instance VRM
		{
			get
			{
				if (!m_Vrm.Key)
				{
					m_Vrm = new KeyValuePair<bool, Vrm10Instance>(true, GetComponent<Vrm10Instance>());
				}
				return m_Vrm.Value;
			}
		}
		public Vrm10RuntimeExpression Expression => VRM.Runtime.Expression;


		[SerializeField, Range(0f, 1f)] float m_Happy = 0f;
		[SerializeField, Range(0f, 1f)] float m_Angry = 0f;
		[SerializeField, Range(0f, 1f)] float m_Sad = 0f;
		[SerializeField, Range(0f, 1f)] float m_Relaxed = 0f;
		[SerializeField, Range(0f, 1f)] float m_Surprised = 0f;

		[SerializeField, Range(0f, 1f)] float m_Blink = 0f;
		[SerializeField, Range(0f, 1f)] float m_BlinkLeft = 0f;
		[SerializeField, Range(0f, 1f)] float m_BlinkRight = 0f;

		[SerializeField, Range(-1f, 1f)] float m_LookVertical = 0f; // up = 1, down = -1
		[SerializeField, Range(-1f, 1f)] float m_LookHorizontal = 0f; // right = 1, left = -1
		private Vector4 GetLookAtParams()
		{
			// Convert vertical/horizontal into up,down, left,right within 0~1 range.
			var up = Mathf.Clamp01(m_LookVertical);
			var dn = Mathf.Clamp01(-m_LookVertical);
			var rt = Mathf.Clamp01(m_LookHorizontal);
			var lt = Mathf.Clamp01(-m_LookHorizontal);

			return new Vector4(up, dn, rt, lt);
		}

		[ContextMenu("Test Face 01")]
		private void Test01()
		{
			Dictionary<ExpressionKey, float> data = new Dictionary<ExpressionKey, float>();
			data.Add(ExpressionKey.Happy, m_Happy);
			data.Add(ExpressionKey.Angry, m_Angry);
			data.Add(ExpressionKey.Sad, m_Sad);
			data.Add(ExpressionKey.Blink, m_Blink);
			Expression.SetWeights(data);
		}
		[ContextMenu("Test Face 02")]
		private void Test02()
		{
			Dictionary<ExpressionKey, float> data = new Dictionary<ExpressionKey, float>();
			// data.Add(ExpressionKey.Happy, m_Happy);
			//data.Add(ExpressionKey.Angry, m_Angry);
			//data.Add(ExpressionKey.Sad, m_Sad);
			data.Add(ExpressionKey.Blink, m_Blink);
			///data.Add(ExpressionKey.LookUp, m_LookVertical);
			Expression.SetWeights(data);
		}

		[ContextMenu("Test Face 03")]
		private void Test03()
		{
			var arr = Expression.ActualWeights.ToArray();
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"Total expression {arr.Length}");
			for (int i = 0; i < arr.Length; ++i)
			{
				var item = arr[i].Key;
				var val = arr[i].Value;
				sb.Append($"[{item.Name}] {val:P2}");
				sb.Append(", IsProcedual = ").Append(item.IsProcedual);
				sb.Append(", IsBlink = ").Append(item.IsBlink);
				sb.Append(", IsLookAt = ").Append(item.IsLookAt);
				sb.Append(", IsMouth = ").Append(item.IsMouth);
				sb.AppendLine();
			}
			Debug.Log(sb.ToString());
		}

		[SerializeField] bool m_DisableExpressionCtrl = false;

		List<ExpressionKey> m_LipSync = null;

		private void FetchExpreession()
		{
			var arr = Expression.GetWeights().ToArray();
			m_LipSync = new List<ExpressionKey>();
			for (int i = 0; i < arr.Length; ++i)
			{
				var item = arr[i].Key;
				if (item.IsMouth)
				{
					m_LipSync.Add(item);
				}
			}
		}
		private void HandleExpression()
		{
			if (m_DisableExpressionCtrl)
				return;


			var data = new Dictionary<ExpressionKey, float>();
			if (ExpressionKey.LookUp	.IsLookAt ||
				ExpressionKey.LookDown	.IsLookAt ||
				ExpressionKey.LookLeft	.IsLookAt ||
				ExpressionKey.LookRight	.IsLookAt)
			{
				var udrl = GetLookAtParams();
				data.Add(ExpressionKey.LookUp,		udrl.x);
				data.Add(ExpressionKey.LookDown,	udrl.y);
				data.Add(ExpressionKey.LookRight,	udrl.z);
				data.Add(ExpressionKey.LookLeft,	udrl.w);
			}

			if (ExpressionKey.BlinkLeft	.IsBlink ||
				ExpressionKey.BlinkRight.IsBlink)
			{
				data.Add(ExpressionKey.BlinkLeft, m_BlinkLeft);
				data.Add(ExpressionKey.BlinkRight, m_BlinkRight);
			}
			else if (ExpressionKey.Blink.IsBlink)
			{
				var max = Mathf.Max(m_BlinkLeft, m_BlinkRight);
				data.Add(ExpressionKey.Blink, max);
			}

			// A, E, I, O, U

			Expression.SetWeights(data);
		}

		#region Blink
		[System.Serializable]
		public class BlinkConfig
		{
			[MinMaxSlider(0f, 60f)]
			[Help("human blink 15~20times per min, 60/15=4sec, 60/20=3sec")]
			public Vector2 intervalRange = new Vector2(3f, 4f);
			[MinMaxSlider(0f, 1f)]
			[Help("the duration of single blink animation (seconds)")]
			public Vector2 durationRange = new Vector2(0.25f, 0.5f);
			[Range(0f, 1f)]
			public float doubleBlinkChance = 0.3f;

			public AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		}
		public enum eBlinkState
		{
			Idle,
			Blinking,
			Distract,
		}
		private struct BlinkInfo
		{
			public float lastStartTime, lastEndTime;
			public float rndNextDuration;
			public bool focusMode;
			public string debuglog;
		}
		private BlinkInfo m_BlinkInfo;
		[SerializeField]
		public BlinkConfig m_BlinkConfig = new BlinkConfig();

		private const float s_MaxWeight = 1f;
		public Vector2 GetBlinkCap()
		{
			// TODO: allow modify the max blink cap for each eyes.
			return Vector2.one;
			//if (m_Database == null || m_Database.data == null)
			//	return Vector2.zero;
			//var cnt = m_Database.data.Length;
			//var left = 1f;
			//var right = 1f;
			//for (int i = 0; i < cnt; ++i)
			//{
			//	var weight01 = GetBlendShapeWeight01(i);
			//	if (weight01 < 0.1f)
			//		continue; // ignore
			//	var l = Mathf.Clamp01(1f - m_Database.data[i].leftBlinkCap);
			//	var r = Mathf.Clamp01(1f - m_Database.data[i].rightBlinkCap);
			//	if (left > l)
			//		left = l;
			//	if (right > r)
			//		right = r;
			//}
			//return new Vector2(left, right);
		}
		private void HandleBlinkUpdate()
		{
			if (IsBlinking)
				return;
			var lastBlinkPassed = Time.timeSinceLevelLoad - m_BlinkInfo.lastEndTime;
			var minInterval = Mathf.Min(m_BlinkConfig.intervalRange.x, m_BlinkConfig.intervalRange.y);
			var shouldTrigger = lastBlinkPassed > m_BlinkInfo.rndNextDuration;
			if (!shouldTrigger &&
				lastBlinkPassed >= minInterval)
			{
				// moved target in sign, blink
				// reach min blink interval, ready to blink.
				//if (m_Target.IsValid)
				//{
				//	shouldTrigger |= m_Target.history.Count < 2; // most likely just changed target
				//	if (!shouldTrigger)
				//	{
				//		var flag = m_Target.ObserverState();
				//		shouldTrigger |= (flag & (TargetInfo.eObserverState.StartMoving | TargetInfo.eObserverState.ChangingDir)) != 0;
				//	}
				//}
			}

			if (!shouldTrigger)
				return;

			var duration = Random.Range(m_BlinkConfig.durationRange.x, m_BlinkConfig.durationRange.y);
			InternalTriggerBlink(duration);
		}
		private void InternalCleanBlink()
		{
			var f = m_BlinkConfig.intervalRange;
			m_BlinkInfo.rndNextDuration = Random.Range(f.x, f.y);  // define next blink duration.
			if (m_BlinkConfig.doubleBlinkChance > float.Epsilon &&
				m_BlinkConfig.doubleBlinkChance >= Random.value)
			{
				// double blink chance
				m_BlinkInfo.rndNextDuration = 0f;
			}
			m_BlinkInfo.lastEndTime = Time.timeSinceLevelLoad;
			if (m_BlinkTask != null)
				StopCoroutine(m_BlinkTask);
			m_BlinkTask = null;
		}
		private void InternalTriggerBlink(float duration)
		{
			m_BlinkInfo.lastStartTime = Time.timeSinceLevelLoad;
			m_BlinkTask = StartCoroutine(CoBlinkHandler(duration));
		}

		public bool TryTriggerBlink(float duration)
		{
			if (IsBlinking)
				return false;
			InternalTriggerBlink(duration);
			return true;
		}

		public bool IsBlinking => m_BlinkTask != null;
		private Coroutine m_BlinkTask = null;
		private IEnumerator CoBlinkHandler(float duration)
		{
			var config = m_BlinkConfig;
			var halfDuration = duration * 0.5f;
			var cnt = 0;

			for (var pass = 0f; pass < halfDuration; pass += Time.deltaTime)
			{
				var pt = config.blinkCurve.Evaluate(Mathf.Clamp01(pass / halfDuration));
				var dst = Mathf.Lerp(0f, s_MaxWeight, pt);
				_SetBlink(dst);
				m_BlinkInfo.debuglog = $"closing = dst={dst:F2}, pt={pt:F2}, {++cnt}";
				yield return null; // new WaitForEndOfFrame();

			}

			_SetBlink(s_MaxWeight);
			for (var pass = halfDuration; pass > 0f; pass -= Time.deltaTime)
			{
				var pt = config.blinkCurve.Evaluate(Mathf.Clamp01(pass / halfDuration));
				var dst = Mathf.Lerp(0f, s_MaxWeight, pt);
				_SetBlink(dst);
				m_BlinkInfo.debuglog = $"opening = dst={dst:F2}, pt={pt:F2}, {++cnt}";
				yield return null; // new WaitForEndOfFrame();
			}
			m_BlinkInfo.debuglog = $"End - {Time.timeSinceLevelLoad:F4}";
			_SetBlink(0f);

			InternalCleanBlink();
			void _SetBlink(float weight01)
			{
				var caps = GetBlinkCap();
				m_BlinkLeft = Mathf.Min(weight01, caps.x);
				m_BlinkRight = Mathf.Min(weight01, caps.y);
			}
		}
		#endregion Blink
	}
} 