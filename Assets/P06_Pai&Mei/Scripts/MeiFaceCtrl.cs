using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[System.Obsolete("Use FaceRig instead.")]
	public class MeiFaceCtrl : MonoBehaviour, IBlendShapeRequest
    {
        [SerializeField] BodyLayout bodyLayout;
		[SerializeField] Transform m_Head;
		private Transform head
		{
			get
			{
				if (m_Head == null)
					m_Head = bodyLayout.animator.GetBoneTransform(HumanBodyBones.Head);
				return m_Head;
			}
		}
		[SerializeField] Transform m_lEye, m_rEye;
		private Transform lEye
		{
			get
			{
				if (m_lEye == null)
					m_lEye = bodyLayout.animator.GetBoneTransform(HumanBodyBones.LeftEye);
				return m_lEye;
			}
		}
		private Transform rEye
		{
			get
			{
				if (m_rEye == null)
					m_rEye = bodyLayout.animator.GetBoneTransform(HumanBodyBones.RightEye);
				return m_rEye;
			}
		}
		[Header("Serialize Data")]
		[SerializeField] Quaternion m_lRotFix;
		[SerializeField] Quaternion m_rRotFix;
		[SerializeField] Quaternion m_hRotFix;

		private FaceRig faceRig => bodyLayout?.faceRig;
		[SerializeField] float m_EyeSignClampAngle = 30f;

		private void Reset()
		{
			FetchFacingData();
		}

		private void OnValidate()
		{
			FetchFacingData();
		}

		[ContextMenu("Fetch Facing Data")]
		private void FetchFacingData()
		{
			if (bodyLayout == null)
				return;
			var root = bodyLayout.transform.rotation;

			// We assume first frame is the default pose.
			// Head's vision should align body's forward, 
			// so we can calculate the rotation offset.
			// and keep the different between body forward, head's forward.
			m_hRotFix = root.Inverse() * head.rotation;
			m_lRotFix = root.Inverse() * lEye.rotation;
			m_rRotFix = root.Inverse() * rEye.rotation;
		}

		private void OnEnable()
		{
			m_BlinkInfo = default;
			InternalCleanBlink();
			faceRig.Register(this);
		}
		private void OnDisable()
		{
			faceRig.Unregister(this);
			InternalCleanBlink();
		}

		private void Update()
		{
			HandleBlinkUpdate();
		}

		private void FixedUpdate()
		{
			HandleObserveTarget();
		}

		private void LateUpdate()
		{
			HandleLookAtUpdate();
		}

		private void OnDrawGizmos()
		{
			if (!HasTarget)
				return;
			
			var txt = $"IsBlinking = {IsBlinking}\n{GetBlinkDebug()}\n" + m_Target.ObserverDebug();
			var hPos = head ? head.transform.position : transform.position;

			InternalDefineFocus(
				out var tPos,
				out var vectorRaw,
				out var vector,
				out var quat,
				out var offset2d,
				out var offset3d);

			if (tPos != null)
			{
				var c = m_DistractConfig.debugColor;
				GizmosExtend.DrawCircle(tPos, quat * Vector3.forward, c, offset2d.magnitude);
				GizmosExtend.DrawLine(hPos, tPos + offset3d, c);
				GizmosExtend.DrawLine(tPos, tPos + offset3d, c);
			}

			// label
			GizmosExtend.DrawLabel(hPos, txt, Vector2.right * 0.2f);
		}

		#region Target
		[System.Serializable]
		public class TargetInfo
		{
			public bool IsValid => target != null;
			public enum eState
			{
				Free = 0,
				LookAt,
				LookAway,
			}

			// the target to look at
			public Transform target = null;
			// the focus weight for looking at target, 0~1
			[Range(0f, 1f)] public float weight01 = 0.75f;
			// the speed of looking at target (Time.deltaTime)
			[Min(0f)] public float speed = 5f;

			public eState state = eState.LookAt;

			public CircularBuffer<ObserverInfo> history { get; private set; }
			public void Observer()
			{
				if (target == null)
					return;

				if (history == null)
					history = new CircularBuffer<ObserverInfo>(s_ObserverRecordSize);

				var hasLast = history.Count > 0;
				var last = hasLast ? history.InvPeek() : ObserverInfo.Invalid;
				var motion = hasLast ? target.position - last.position : Vector3.zero;

				history.Enqueue(new ObserverInfo
				{
					lastTime = Time.timeSinceLevelLoad,
					position = target.position,
					motion = motion,
					moveLength = motion.magnitude,
				});
			}

			[System.Flags]
			public enum eObserverState
			{
				Idle = 0,
				StartMoving = 1 << 1,
				KeepMoving = 1 << 2,
				ChangingDir = 1 << 3,
			}
			private KeyValuePair<int, eObserverState> m_ObserverState;
			public eObserverState ObserverState()
			{
				if (history == null || history.Count < 2)
					return UpdateValue(eObserverState.Idle);

				if (Time.frameCount == m_ObserverState.Key)
					return m_ObserverState.Value;

				if (!(history.TryPeek(-1, out var l0) && history.TryPeek(-2, out var l1)))
					return UpdateValue(eObserverState.Idle);

				DebugExtend.DrawRay(l0.position, -l0.motion, Color.red, Time.fixedDeltaTime);
				DebugExtend.DrawRay(l1.position, -l1.motion, Color.green, Time.fixedDeltaTime);

				eObserverState rst = default;
				const float threshold = 0.0000001f;
				var idle0 = l0.moveLength < threshold;
				var idle1 = l1.moveLength < threshold;
				var startMoving = idle0 && !idle1;
				if (startMoving)
					rst |= eObserverState.StartMoving;

				var keepMoving = !idle0 && !idle1;
				if (keepMoving)
					rst |= eObserverState.KeepMoving;

				var changeDir = keepMoving && Vector3.Dot(l0.motion, l1.motion) < 0f;
				if (changeDir)
					rst |= eObserverState.ChangingDir;

				return UpdateValue(rst);
				eObserverState UpdateValue(eObserverState val)
				{
					m_ObserverState = new KeyValuePair<int, eObserverState>(Time.frameCount, val);
					return val;
				}
			}

			public string ObserverDebug()
			{
				var summary = ObserverState().ToString();
				var _tname = target ? target.name : "Unknown";
				var _hCnt = history != null ? $"{history.Count:00}" : "NaN";
				return $"T:\"{_tname}\", record:[{_hCnt}],\n{summary}";
			}
		}
		[SerializeField] TargetInfo m_Target = new TargetInfo();
		private TargetInfo m_PreviousTarget = null;

		[System.Serializable]
		public class DistractConfig
		{
			// how often being distract from giving target
			[Min(0f)] public Vector2 intervalRange = new Vector2(0.3f, 1.8f);

			// the new focus target around giving target radius.
			[Min(0f)] public Vector2 radius = new Vector2(0.001f, 0.004f);

			public Color debugColor = Color.yellow.CloneAlpha(0.3f);
		}
		[SerializeField] DistractConfig m_DistractConfig = new DistractConfig();
		private struct DistractInfo
		{
			public float lastTime;
			public float cooldown;
			public Vector2 offset;
		}
		private DistractInfo m_DistractInfo = default;
		private Vector2 GetDistractionOffset()
		{
			if (Time.timeSinceLevelLoad - m_DistractInfo.lastTime < m_DistractInfo.cooldown)
				return m_DistractInfo.offset;

			var interval	= Random.Range(m_DistractConfig.intervalRange.x, m_DistractConfig.intervalRange.y);
			var radius		= Random.Range(m_DistractConfig.radius.x, m_DistractConfig.radius.y);
			var offset		= Random.insideUnitCircle * radius;
			m_DistractInfo	= new DistractInfo
			{
				lastTime	= Time.timeSinceLevelLoad,
				cooldown	= interval,
				offset		= offset,
			};
			return offset;
		}
		/// <summary></summary>
		/// <param name="target"></param>
		/// <param name="lookAway"></param>
		/// <param name="weight01"></param>
		public void SetTarget(Transform target, bool lookAway, float weight01)
		{
			m_PreviousTarget = m_Target;
			m_Target = new TargetInfo
			{
				target = target,
				weight01 = weight01,
				state = lookAway ? TargetInfo.eState.LookAt : TargetInfo.eState.LookAway,
			};
		}
		public void ClearTarget()
		{
			m_PreviousTarget = m_Target;
			m_Target = null;
		}
		private bool HasTarget => m_Target != null && m_Target.IsValid;
		private bool HasPreviousTarget => m_PreviousTarget != null && m_PreviousTarget.IsValid;

		public struct ObserverInfo
		{
			public static readonly ObserverInfo Invalid = default;
			public float lastTime;
			public Vector3 position;
			public Vector3 motion;
			public float moveLength;
		}
		private const int s_ObserverRecordSize = 30;
		private void HandleObserveTarget()
		{
			if (!HasTarget)
				return;

			// Note: Run in fixed update.
			m_Target.Observer();
		}
		#endregion Target


		#region LookAt
		private Vector3 m_LastEyeVector = Vector3.zero;
		private void HandleLookAtUpdate()
		{
			//if (head == null)
			//	return;

			if (!m_Target.IsValid)
				return;

			var weight = m_Target.weight01;
			var weightFlipped	= Mathf.Clamp01(1f - m_Target.weight01);

			InternalDefineFocus(
				out var tPos,
				out var vectorRaw,
				out var vector,
				out var quat,
				out var offset2d,
				out var offset3d);

			var org = head.position;
			var dir = tPos - org;
			if (dir == Vector3.zero)
				return;

			InternalLookDir(vector);
		}
		private void InternalDefineFocus(
			out Vector3 tPos,out Vector3 vectorRaw, out Vector3 vector, out Quaternion quat, out Vector2 offset2d, out Vector3 offset3d)
		{
			if (m_Target == null || m_Target.target == null)
				throw new System.ArgumentNullException(nameof(m_Target));
			if (head == null)
				throw new System.ArgumentNullException(nameof(head));
			
			var hPos		= head.position;
			tPos			= m_Target.target.position;
			vectorRaw		= tPos - hPos;
			var headFwdQuat = head.rotation * m_hRotFix;
			quat			= vectorRaw == Vector3.zero ? headFwdQuat : Quaternion.LookRotation(-vectorRaw, head.up);
			offset2d		= GetDistractionOffset() * Mathf.Clamp01(1f - m_Target.weight01);
			offset3d		= quat * (Vector3)offset2d;

			switch(m_Target.state)
			{
				case TargetInfo.eState.LookAt:
					vector = (offset3d + tPos) - hPos;
					break;
				case TargetInfo.eState.LookAway:
					{
						var dir = vectorRaw.normalized;
						var headFwd = headFwdQuat * Vector3.forward;
						var dot = Vector3.Dot(dir, headFwd);
						if (dot == -1f || dot == 1f)
						{
							// perfect align
							vector = (Vector3)GetDistractionOffset();
						}
						else if (dot > 0.71f || dot < -0.71f)
						{
							vector = Vector3.ProjectOnPlane(-vectorRaw, headFwd).normalized;
						}
						else
						{
							vector = Vector3.Reflect(-vectorRaw.normalized, headFwd);
						}
					}
					break;
				case TargetInfo.eState.Free:
					vector = headFwdQuat * quat * GetDistractionOffset();
					break;
				default:
					throw new System.NotImplementedException();
			};
		}
		private void InternalLookDir(Vector3 v)
		{
			var speed = m_Target.speed;
			var headPos = head.position;
			var headRot = (head.rotation * m_hRotFix.Inverse());
			var headFwd = headRot * Vector3.forward;
			var headUp	= headRot * Vector3.up;
			// Check head's rotation
			//DebugExtend.DrawRay(headPos, headFwd, Color.blue);
			//DebugExtend.DrawRay(headPos, headUp, Color.green);
			if (v == Vector3.zero)
				v = headFwd;

			var angle = Vector3.Angle(headFwd, v);
			var maxAngle = Mathf.Clamp(m_EyeSignClampAngle, 0f, 180f);
			if (angle > maxAngle)
			{
				// DebugExtend.DrawRay(headPos, v, Color.gray);
				v = Vector3.RotateTowards(headFwd, v, Mathf.Deg2Rad * maxAngle, 360f);
				// DebugExtend.DrawRay(headPos, v, Color.yellow);
			}

			var v3d = m_LastEyeVector = Vector3.Lerp(m_LastEyeVector, v, Time.deltaTime * speed);
			var quat = Quaternion.LookRotation(v3d, headUp);
			if (lEye) lEye.rotation = quat * m_lRotFix;
			if (rEye) rEye.rotation = quat * m_rRotFix;
		}

		#endregion LookAt

		#region Blink
		const float s_MaxWeight = 1f;

		private float[] m_Cache = new float[System.Enum.GetValues(typeof(eHeroineFaceRig)).Length];
		private HashSet<int> m_ModifiedIndex = new HashSet<int>(16);
		private void Set(eMeiFaceRig rig, float weight)
		{
			var idx = (int)rig;
			m_Cache[idx] = weight;
			if (!m_ModifiedIndex.Contains(idx))
				m_ModifiedIndex.Add(idx);
		}

		[System.Serializable]
		private class BlinkConfig
		{
			[MinMaxSlider(0f, 60f)]
			// human blink 15~20times per min, 60/15=4sec, 60/20=3sec
			public Vector2 intervalRange = new Vector2(3f, 4f);
			[MinMaxSlider(0f, 1f)]
			// the duration of single blink animation (seconds)
			public Vector2 durationRange = new Vector2(0.25f, 0.5f);
			[Range(0f, 1f)]
			public float doubleBlinkChance = 0.3f;

			public AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		}
		private struct BlinkInfo
		{
			public float lastStartTime, lastEndTime;
			public float rndNextDuration;
			public string debuglog;
		}
		private BlinkInfo m_BlinkInfo;
		[SerializeField] BlinkConfig m_BlinkConfig;
		private void HandleBlinkUpdate()
		{
			if (IsBlinking)
				return;
			var lastBlinkPassed = Time.timeSinceLevelLoad - m_BlinkInfo.lastEndTime;
			var minInterval = Mathf.Min(m_BlinkConfig.intervalRange.x, m_BlinkConfig.intervalRange.y);
			var triggerBlink = lastBlinkPassed > m_BlinkInfo.rndNextDuration;
			if (!triggerBlink &&
				HasTarget &&
				lastBlinkPassed >= minInterval)
			{
				// reach min blink interval, ready to blink.
				triggerBlink |= m_Target.history.Count < 2; // most likely just changed target
				if (!triggerBlink)
				{
					var flag = m_Target.ObserverState();
					triggerBlink |= (flag & (TargetInfo.eObserverState.StartMoving | TargetInfo.eObserverState.ChangingDir)) != 0;
				}
			}

			if (!triggerBlink)
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
		public bool IsBlinking => m_BlinkTask != null;
		private Coroutine m_BlinkTask = null;
		private IEnumerator CoBlinkHandler(float duration)
		{
			var config = m_BlinkConfig;
			void _SetBlink(float weight)
			{
				Set(eMeiFaceRig.Blink_L, weight);
				Set(eMeiFaceRig.Blink_R, weight);
			}

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
		}

		private string GetBlinkDebug()
		{
			if (!Application.isPlaying)
				return string.Empty;

			if (IsBlinking)
			{
				return m_BlinkInfo.debuglog;
			}
			else
			{
				var pass = Time.timeSinceLevelLoad - m_BlinkInfo.lastEndTime;
				var eta = Mathf.Max(0f, m_BlinkConfig.intervalRange.x - pass);
				var eta2 = Mathf.Max(m_BlinkInfo.rndNextDuration - pass);
				return $"ETA: {eta:F1} ~ {eta2:F1}sec";
			}
		}

		#endregion Blink

		#region FaceRig Parameters
		float IBlendShapeRequest.GetBlendWeight()
		{
			return enabled ? 1f : 0f;
		}

		IEnumerable<BlendShapeRequest> IBlendShapeRequest.GetBlendShapeRequests()
		{
			if (m_ModifiedIndex.Count == 0)
				yield break;
			foreach(var idx in m_ModifiedIndex)
			{
				yield return new BlendShapeRequest(idx, m_Cache[idx]);
			}
			// Consume each frame(s)
			m_ModifiedIndex.Clear();
		}
		#endregion FaceRig Parameters
	}
}