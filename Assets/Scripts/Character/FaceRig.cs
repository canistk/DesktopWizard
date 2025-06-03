using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
	using eShapeType = FaceRigDatabase.eShapeType;
	[System.Serializable]
	public struct BlendShapeRequest
	{
		/// <summary>Process this request</summary>
		public bool enable;

		/// <summary>The blend shape's index on mesh</summary>
		public int index;

		/// <summary>The value assigned to target blend shape.
		/// normalize to 0 ~ 1.</summary>
		public float weight01;

		public BlendShapeRequest(int index, float weight01) :
			this(index, weight01, true)
		{}
		public BlendShapeRequest(int rig, float weight01, bool enable)
		{
			this.enable	= enable;
			this.index	= rig;
			this.weight01 = Mathf.Clamp01(weight01);
		}
	}

	[System.Serializable]
	public struct BlendShapeLookAt
	{
		public int up;
		public int down;
		public int left;
		public int right;
	}

	public interface IBlendShapeRequest
	{
		float GetBlendWeight();
		IEnumerable<BlendShapeRequest> GetBlendShapeRequests();
	}
	public enum eEyeMethod
	{
		BlendShape,
		Transform,
	}

	[RequireComponent(typeof(SkinnedMeshRenderer))]
	public class FaceRig : MonoBehaviour, IBlendShapeRequest
	{
		[SerializeField] SkinnedMeshRenderer m_SkinnedMeshRenderer = null;
		private SkinnedMeshRenderer render
		{
			get
			{
				if (m_SkinnedMeshRenderer == null)
					m_SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
				return m_SkinnedMeshRenderer;
			}
		}
		[SerializeField] Animator m_Animator = null;
		private Animator animator
		{
			get
			{
				if (m_Animator == null)
					m_Animator = GetComponentInParent<Animator>();
				return m_Animator;
			}
		}
		/// <summary>
		/// In order to define head position and rotation
		/// assume Z-axis is the forward direction of the head.
		/// </summary>
		[SerializeField] public FaceRigDatabase m_Database = null;

		[SerializeField] public Transform m_Head = null;
		private Transform head
		{
			get
			{
				if (m_Head == null)
					m_Head = animator.GetBoneTransform(HumanBodyBones.Head);
				return m_Head;
			}
		}
		[Header("Blink eye")]
		[SerializeField] public int m_BlinkLeftEye = 0, m_BlinkRightEye = 0;

		[SerializeField] public eEyeMethod m_EyeMethod = eEyeMethod.BlendShape;

		[Tooltip("For blend shape max value.")]
		[SerializeField, Min(0f)] public float m_BS_MaxWeight = 1f;
		/// <summary>For blend shape to multiple the axis project value.</summary>
		[Tooltip("For blend shape to multiple the axis project value.")]
		[SerializeField, Min(1f)] public float m_BS_YAxisBias = 3f;
		/// <summary>For blend shape to multiple the axis project value.</summary>
		[Tooltip("For blend shape to multiple the axis project value.")]
		[SerializeField, Min(1f)] public float m_BS_XAxisBias = 2f;

		public BlendShapeLookAt m_BS_LeftEye;
		public BlendShapeLookAt m_BS_RightEye;

		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;

		#region Mono

		private void Reset()
		{
			m_SkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
		}
		private void OnEnable()
		{
			m_BlinkInfo = default;
			InternalCleanBlink();

			// for EyeMethod BlendShape
			Register(this);
		}
		private void OnDisable()
		{
			// for EyeMethod BlendShape
			Unregister(this);

			InternalCleanBlink();
		}

		private void Update()
		{
			HandleBlinkUpdate();

			HandleLookAtUpdate();
			
			HandleBlendShape();
		}

		private void FixedUpdate()
		{
			HandleObserveTarget();
		}

		private void OnDrawGizmos()
		{
			var txt = $"IsBlinking = {IsBlinking}\n{GetBlinkDebug()}";
			if (HasTarget)
				txt += "\n" + m_Target.ObserverDebug();
			var hPos = head ? head.transform.position :
				animator ? animator.transform.position :
				transform.position;
			if (m_Target == null || m_Target.target == null || head == null)
				return;
			InternalDefineFocus(
				out var tPos,
				out var vectorRaw,
				out var vector,
				out var quat,
				out var offset2d,
				out var offset3d);

			var c = m_DistractConfig.debugColor;
			GizmosExtend.DrawCircle(tPos, quat * Vector3.forward, c, offset2d.magnitude);
			GizmosExtend.DrawLine(hPos, tPos + offset3d, c);
			GizmosExtend.DrawLine(tPos, tPos + offset3d, c);

			GizmosExtend.DrawLabel(hPos, txt, Vector2.right * 0.2f);
		}
		#endregion Mono

		#region Editor
		[ContextMenu("Fetch blendshape to Database")]
		public void Editor_FetchDatabase()
		{
			if (m_Database == null)
			{
				Debug.LogError("Database not assigned.");
#if UNITY_EDITOR
				var ans = UnityEditor.EditorUtility.DisplayDialog("Error", "Database not assigned, do you want to create a new one?", "OK", "No thanks");
				if (ans)
				{
					var path = UnityEditor.EditorUtility.SaveFilePanelInProject("Create FaceRigDatabase", "FaceRigDatabase", "asset", "Please enter a file name to save the database to");
					if (string.IsNullOrEmpty(path))
						return;
					m_Database = ScriptableObject.CreateInstance<FaceRigDatabase>();
					UnityEditor.AssetDatabase.CreateAsset(m_Database, path);
					UnityEditor.AssetDatabase.SaveAssets();
					UnityEditor.AssetDatabase.Refresh();
				}
#endif
				if (!ans)
					return; // skip create process
			}

			var cnt = render.sharedMesh.blendShapeCount;
			if (m_Database.data == null || m_Database.data.Length != cnt)
			{
				m_Database.data = new FaceRigDatabase.ShapeInfo[cnt];
			}
			var db = m_Database.data;
			for (int i = 0; i < cnt; ++i)
			{
				var _blendShapeName = render.sharedMesh.GetBlendShapeName(i);
				db[i].name = _blendShapeName;
				db[i].index = i;
				
				var isLeft = _blendShapeName.Contains("left", IGNORE);
				var isRight = _blendShapeName.Contains("right", IGNORE);
				if (_blendShapeName.Contains("eye", IGNORE))
				{
					if (isLeft)
						db[i].shapeType |= eShapeType.LeftEye;
					if (isRight)
						db[i].shapeType |= eShapeType.RightEye;
				}
				else if (_blendShapeName.Contains("brow", IGNORE))
				{
					if (isLeft)
						db[i].shapeType |= eShapeType.BrowLeft;
					if (isRight)
						db[i].shapeType |= eShapeType.BrowRight;
				}
				else if (_blendShapeName.Contains("cheek", IGNORE))
				{
					db[i].shapeType |= eShapeType.Cheek;
				}
				else if (_blendShapeName.Contains("mouth", IGNORE))
				{
					db[i].shapeType |= eShapeType.Mouth;
				}
				else if (_blendShapeName.Contains("nose", IGNORE))
				{
					db[i].shapeType |= eShapeType.Nose;
				}
				else if (_blendShapeName.Contains("tongue", IGNORE))
				{
					db[i].shapeType |= eShapeType.Tongue;
				}
				else if (_blendShapeName.Contains("jaw", IGNORE))
				{
					db[i].shapeType |= eShapeType.Jaw;
				}

				if (i >= 58 && i <= 74)
				{
					db[i].shapeType |= eShapeType.Face;
				}
			}
		}
		#endregion Editor

		#region Handle BlendShape(s)

		private List<IBlendShapeRequest> providers = new List<IBlendShapeRequest>();
		public void Register(IBlendShapeRequest source) => providers.Add(source);
		public void Unregister(IBlendShapeRequest source) => providers.Remove(source);

		public int GetBlendShapeCount() => render?.sharedMesh?.blendShapeCount ?? 0;

		public float GetBlendShapeWeight01(int index)
		{
			if (render == null || render.sharedMesh == null)
				return 0f;
			if (index < 0 || index >= GetBlendShapeCount())
				throw new System.IndexOutOfRangeException();
			var value = render.GetBlendShapeWeight(index);
			return Mathf.InverseLerp(0f, m_BS_MaxWeight, value);
		}

		public bool TryGetBlendShapeInfo(int index, out FaceRigDatabase.ShapeInfo result)
		{
			result = default;
			if (m_Database == null || m_Database.data == null || m_Database.data.Length <= index)
				return false;
			result = m_Database.data[index];
			return true;
		}

		public bool IsBlendShapeTypeInUsed(eShapeType shapeType, params int[] skipIndex)
		{
			if (shapeType == 0)
				return false;
			var hash = new HashSet<int>(skipIndex);
			var cnt = m_Database.data.Length;
			for (int i = 0; i < cnt; ++i)
			{
				if (hash.Contains(i))
					continue; // skip this index
				var weight01 = GetBlendShapeWeight01(i);
				if (weight01 < 0.1f)
					continue; // ignore
				
				if ((m_Database.data[i].shapeType & shapeType) != 0)
					return true;
			}
			return false;
		}

		public Vector2 GetBlinkCap()
		{
			if (m_Database == null || m_Database.data == null)
				return Vector2.zero;
			var cnt = m_Database.data.Length;
			var left = 1f;
			var right = 1f;
			for (int i = 0; i < cnt; ++i)
			{
				var weight01 = GetBlendShapeWeight01(i);
				if (weight01 < 0.1f)
					continue; // ignore
				var l = Mathf.Clamp01(1f - m_Database.data[i].leftBlinkCap);
				var r = Mathf.Clamp01(1f - m_Database.data[i].rightBlinkCap);
				if (left > l)
					left = l;
				if (right > r)
					right = r;
			}
			return new Vector2(left, right);
		}

		private class RequestCentroid
		{
			public readonly int index;
			public float totalWeight;
			public float goal;

			public RequestCentroid(int index)
			{
				this.index = index;
			}

			/// <summary></summary>
			/// <param name="reqest">request</param>
			/// <param name="blendWeight">provider's blend weight</param>
			public void Apply(BlendShapeRequest reqest, float blendWeight)
			{
				totalWeight += blendWeight;
				goal += reqest.weight01 * blendWeight;
			}

			public float percentage => totalWeight == 0f ? 0f : Mathf.Clamp01(goal / totalWeight);
		}

		private Dictionary<int, RequestCentroid> m_LastRequests;

		private void HandleBlendShape()
		{
			if (providers.Count == 0)
				return;
			if (render == null || render.sharedMesh == null)
				return;

			if (m_LastRequests == null)
				m_LastRequests = new Dictionary<int, RequestCentroid>(8);
			else
				m_LastRequests.Clear();

			int pCnt = providers.Count;
			
			/// go though all <see cref="IBlendShapeRequest"/> 
			/// collect all request.
			for (int p = 0; p < pCnt; ++p)
			{
				// each providers, may allow to request multiple requests.
				foreach (var req in providers[p].GetBlendShapeRequests())
				{
					if (!req.enable)
						continue;

					// blendShape index
					if (!m_LastRequests.TryGetValue(req.index, out var cache))
					{
						cache = new RequestCentroid(req.index);
						m_LastRequests.Add(req.index, cache);
					}

					// accumulate request blend weight from all sources.
					// only accept 0~1 range.
					// the provider should manager the request(s)
					// include not sent when it's not necessary to process the request.
					var w01 = Mathf.Clamp01(providers[p].GetBlendWeight());
					cache.Apply(req, w01);
				}
			}

			if (m_LastRequests.Count == 0)
				return;

			// each blend shape
			int sCnt = GetBlendShapeCount();
			for (int i = 0; i < sCnt; ++i)
			{
				if (!m_LastRequests.TryGetValue(i, out var r))
				{
					// skip when no source request to modify this blend shape.
					continue;
				}

				// calculate centroid weight from all sources.
				Debug.Assert(r.totalWeight > 0f, "Unexpected situation, total weight should be positive.");
				var pt = r.percentage;
				
				// convert 0-1 to 0-100 range.
				pt = Mathf.Clamp(pt * m_BS_MaxWeight, 0f, m_BS_MaxWeight);

				render.SetBlendShapeWeight(i, pt);
			}

			// TODO: don't clean let Gizmos do things.
			// m_LastRequests.Clear();
		}
		#endregion Handle BlendShape(s)

		#region IBlendShapeRequest
		private float[] m_Cache = null;
		private HashSet<int> m_ModifiedIndex = new HashSet<int>(16);
		private void SetBlendShape(int blendShapeIndex, float weight)
		{
			if (blendShapeIndex == -1)
				return;
			if (m_Cache == null)
				m_Cache = new float[render.sharedMesh.blendShapeCount];
			if (blendShapeIndex < 0 || blendShapeIndex >= m_Cache.Length)
				throw new System.IndexOutOfRangeException($"BlendShape index out of range: {blendShapeIndex}/{render.sharedMesh.blendShapeCount}");
			m_Cache[blendShapeIndex] = weight;
			if (!m_ModifiedIndex.Contains(blendShapeIndex))
				m_ModifiedIndex.Add(blendShapeIndex);
		}

		public float GetBlendWeight()
		{
			return 1f;
		}

		public IEnumerable<BlendShapeRequest> GetBlendShapeRequests()
		{
			if (m_ModifiedIndex.Count == 0)
				yield break;
			foreach (var idx in m_ModifiedIndex)
			{
				yield return new BlendShapeRequest(idx, m_Cache[idx]);
			}
			// Consume each frame(s)
			m_ModifiedIndex.Clear();
		}
		#endregion IBlendShapeRequest

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
		[SerializeField] public BlinkConfig m_BlinkConfig;

		private const float s_MaxWeight = 1f;
		
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
				// reach min blink interval, ready to blink.
				if (m_Target.IsValid)
				{
					shouldTrigger |= m_Target.history.Count < 2; // most likely just changed target
					if (!shouldTrigger)
					{
						var flag = m_Target.ObserverState();
						shouldTrigger |= (flag & (TargetInfo.eObserverState.StartMoving | TargetInfo.eObserverState.ChangingDir)) != 0;
					}
				}
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
				if (m_BlinkLeftEye > -1)
				{
					var w = Mathf.Min(weight01, caps.x);
					SetBlendShape(m_BlinkLeftEye, weight01);
				}

				if (m_BlinkRightEye > -1)
				{
					var w = Mathf.Min(weight01, caps.y);
					SetBlendShape(m_BlinkRightEye, weight01);
				}
			}
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
				var pass	= Time.timeSinceLevelLoad - m_BlinkInfo.lastEndTime;
				var eta		= Mathf.Max(0f, m_BlinkConfig.intervalRange.x - pass);
				var eta2	= Mathf.Max(m_BlinkInfo.rndNextDuration - pass);
				return $"ETA: {eta:F1} ~ {eta2:F1}sec";
			}
		}

		#endregion Blink


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
		[SerializeField] public TargetInfo m_Target = new TargetInfo();
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
		[SerializeField] public DistractConfig m_DistractConfig = new DistractConfig();
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

			var interval = Random.Range(m_DistractConfig.intervalRange.x, m_DistractConfig.intervalRange.y);
			var radius = Random.Range(m_DistractConfig.radius.x, m_DistractConfig.radius.y);
			var offset = Random.insideUnitCircle * radius;
			m_DistractInfo = new DistractInfo
			{
				lastTime = Time.timeSinceLevelLoad,
				cooldown = interval,
				offset = offset,
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

		#region LookAt Target
		private void HandleLookAtUpdate()
		{
			if (head == null)
				return;

			var weight = Mathf.Clamp01(1f - m_Target.weight01);
			if (m_Target == null || m_Target.target == null)
				return;

			InternalDefineFocus(
				out var tPos,
				out var vectorRaw,
				out var vector,
				out var quat,
				out var offset2d,
				out var offset3d);

			switch (m_EyeMethod)
			{
				case eEyeMethod.BlendShape:
				HandleLookAt_BlendShape(vector, vectorRaw);
				break;
				case eEyeMethod.Transform:
				HandleLookAt_Transform(vector);
				break;
				default: throw new System.NotImplementedException();
			}
		}

		private void InternalDefineFocus(
			out Vector3 tPos, out Vector3 vectorRaw, out Vector3 vector, out Quaternion quat, out Vector2 offset2d, out Vector3 offset3d)
		{
			if (m_Target == null || m_Target.target == null)
				throw new System.ArgumentNullException(nameof(m_Target));
			if (head == null)
				throw new System.ArgumentNullException(nameof(head));

			var hPos		= head.position;
			tPos			= m_Target.target.position;
			vectorRaw		= tPos - hPos;
			var headFwdQuat	= head.rotation * m_hRotFix;
			var upward		= headFwdQuat * Vector3.up;
			quat			= vectorRaw == Vector3.zero ? headFwdQuat : Quaternion.LookRotation(-vectorRaw, upward);
			offset2d		= GetDistractionOffset() * Mathf.Clamp01(1f - m_Target.weight01);
			offset3d		= quat * (Vector3)offset2d;

			switch (m_Target.state)
			{
				case TargetInfo.eState.LookAt:
				{
					vector = (offset3d + tPos) - hPos;
				}
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
				{
					vector = headFwdQuat * quat * GetDistractionOffset();
				}
				break;
				default:
				throw new System.NotImplementedException();
			}
		}
		#endregion LookAt Target

		#region Transform LookAt
		[SerializeField] public Transform m_lEye, m_rEye;
		private Transform lEye
		{
			get
			{
				if (m_lEye == null)
					m_lEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
				return m_lEye;
			}
		}
		private Transform rEye
		{
			get
			{
				if (m_rEye == null)
					m_rEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
				return m_rEye;
			}
		}
		[SerializeField] public Quaternion m_lRotFix;
		[SerializeField] public Quaternion m_rRotFix;
		[SerializeField] public Quaternion m_hRotFix;
		[SerializeField] public float m_EyeSignClampAngle = 30f;
		private Vector3 m_LastEyeVector = Vector3.zero;

		[ContextMenu("Fetch head+eye rotation offset")]
		private void FetchFacingData()
		{
			if (animator == null)
			{
				Debug.LogError("No animator found.");
				return;
			}

			var root = animator.transform.rotation;
			// We assume first frame is the default pose.
			// Head's vision should align body's forward, 
			// so we can calculate the rotation offset.
			// and keep the different between body forward, head's forward.
			m_hRotFix = root.Inverse() * head.rotation;
			m_lRotFix = root.Inverse() * lEye.rotation;
			m_rRotFix = root.Inverse() * rEye.rotation;
		}
		private void HandleLookAt_Transform(Vector3 v)
		{
			var speed	= m_Target.speed;
			var headPos	= head.position;
			var headRot	= (head.rotation * m_hRotFix.Inverse());
			var headFwd	= headRot * Vector3.forward;
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
		#endregion Transform LookAt

		#region BlendShape LookAt
		private void HandleLookAt_BlendShape(Vector3 vector, Vector3 vectorRaw)
		{
			if (vector == Vector3.zero)
			{
				InternalLookAt_BlendShape(Vector2.zero);
				return;
			}
			var dot = Vector3.Dot(head.forward, vector);
			if (dot < 0)
			{
				InternalLookAt_BlendShape(Vector2.zero);
				return; // target behind head.
			}

			// Blend shape control
			var fwdLen	= Vector3.Project(vector, head.forward).magnitude;
			var edgeLen = vector.magnitude;
			var rad		= Mathf.Sin(fwdLen / edgeLen);
			var dir		= new Vector2(vectorRaw.x, vectorRaw.y).normalized * s_MaxWeight;
			var bias	= new Vector2(m_BS_XAxisBias, m_BS_YAxisBias);
			var rst		= (1f - rad) * dir * bias;
			InternalLookAt_BlendShape(rst);
		}
		
		private void InternalLookAt_BlendShape(Vector2 v)
		{
			var speed = m_Target.speed;
			var v2d = Vector2.Lerp(m_LastEyeVector, v, Time.deltaTime * speed);
			m_LastEyeVector = v2d;
			if (v2d == Vector2.zero)
			{
				// TODO: set of eyes blend shape mapping
				SetBlendShape(m_BS_LeftEye.down,	0);
				SetBlendShape(m_BS_LeftEye.up,		0);
				SetBlendShape(m_BS_LeftEye.left,	0);
				SetBlendShape(m_BS_LeftEye.right,	0);

				SetBlendShape(m_BS_RightEye.down,	0);
				SetBlendShape(m_BS_RightEye.up,		0);
				SetBlendShape(m_BS_RightEye.left,	0);
				SetBlendShape(m_BS_RightEye.right,	0);
			}
			else
			{
				SetBlendShape(m_BS_LeftEye.down,	Mathf.Min(s_MaxWeight, v2d.y < 0 ? -v2d.y : 0));
				SetBlendShape(m_BS_LeftEye.up,		Mathf.Min(s_MaxWeight, v2d.y >= 0 ? v2d.y : 0));
				SetBlendShape(m_BS_LeftEye.left,	Mathf.Min(s_MaxWeight, v2d.x < 0 ? -v2d.x : 0));
				SetBlendShape(m_BS_LeftEye.right,	Mathf.Min(s_MaxWeight, v2d.x >= 0 ? v2d.x : 0));

				SetBlendShape(m_BS_LeftEye.down,	Mathf.Min(s_MaxWeight, v2d.y < 0 ? -v2d.y : 0));
				SetBlendShape(m_BS_LeftEye.up,		Mathf.Min(s_MaxWeight, v2d.y >= 0 ? v2d.y : 0));
				SetBlendShape(m_BS_LeftEye.left,	Mathf.Min(s_MaxWeight, v2d.x < 0 ? -v2d.x : 0));
				SetBlendShape(m_BS_LeftEye.right,	Mathf.Min(s_MaxWeight, v2d.x >= 0 ? v2d.x : 0));
			}
		}

		#endregion BlendShape LookAt
	}
}