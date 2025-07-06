// #define USE_UNIVRM
// #define USE_ADOBE_TPOSE
using Kit2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public interface IRetarget
	{
		public float GetWeight01();
		public GxRetargeting GetTarget();
		public void OnWillPlayAnimation(IRetarget other);
	}

	[System.Serializable]
	public class TargetInfo : IEquatable<TargetInfo>, IRetarget
	{
		[Range(0f, 1f)] public float weight;
		public float GetWeight01() => Mathf.Clamp01(weight);
		public GxRetargeting target;
		public GxRetargeting GetTarget() => target;

		public bool Equals(TargetInfo other)
		{
			return other != null &&
				Mathf.Approximately(weight, other.weight) &&
				ReferenceEquals(target, other.target);
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj as TargetInfo);
		}

		public override int GetHashCode()
		{
			return (target, weight).GetHashCode();
		}

		public void OnWillPlayAnimation(IRetarget other)
		{
		}
	}

	[RequireComponent(typeof(Animator))]
	public class GxRetargeting : MonoBehaviour
    {
        [SerializeField] Animator m_Animator;
        public Animator animator
        {
            get
            {
                if (m_Animator == null)
                {
                    m_Animator = GetComponent<Animator>();
                }
                return m_Animator;
            }
		}

		[SerializeField] List<TargetInfo> m_Targets = new List<TargetInfo>();
		private List<IRetarget> m_TargetsList = new List<IRetarget>();
		private IEnumerable<IRetarget> targets
		{
			get
			{
				foreach (var t in m_Targets)
				{
					if (t == null || t.target == null)
						continue;
					yield return t;
				}
				foreach (var t in m_TargetsList)
				{
					yield return t;
				}
			}
		}
		private int targetCount
		{
			get
			{
				return m_Targets.Count + m_TargetsList.Count;
			}
		}

		[SerializeField] Transform m_Pivot;
		public Transform pivot => m_Pivot;
		[SerializeField] bool m_RemoveHipMotion = false;
		[SerializeField] Transform[] m_BoneRefs;

		[SerializeField] bool m_LateUpdate = false;
		public bool IsLateUpdate => m_LateUpdate;

		[Header("Extra")]
		[Tooltip("Smooth the pose when applying animations, useful for blending animations.")]
		[SerializeField] bool m_SmoothPose = false;

		[System.Flags]
		private enum eDebugDraw
		{
			TPoseBone = 1 << 0,
			Bone = 1 << 1,
			Rotation = 1 << 2,
		}

		[System.Serializable]
		private struct DebugInfo
		{
			public eDebugDraw gizmos;
			public Color boneColor;
		}
		[SerializeField] DebugInfo m_Debug = new DebugInfo
		{
			boneColor = Color.green,
		};

		private void Awake()
		{
			if (m_BoneRefs == null || m_BoneRefs.Length == 0)
			{
				// Auto initialize bone references, on Awake.
				ForceTPose();
			}
		}

		private void Reset()
		{
			m_Animator = GetComponent<Animator>();
		}

		/// <summary>
		/// based on UniVRM T-Pose, this is the default pose for humanoid avatars.
		/// version VRM-1.0 <see cref="https://vrm.dev/en/univrm1/"/>
		/// </summary>
		private const string s_UniVRM_Tpose_json = @"{""bodyPosition"":{""x"":0.0024022944271564485,""y"":1.0000280141830445,""z"":0.0019842784386128189},""bodyRotation"":{""x"":0.0,""y"":1.4901161193847657e-8,""z"":1.4901161193847657e-8,""w"":1.0},""muscles"":[-1.067216093275647e-8,-6.361109437179021e-16,6.361109437179021e-16,-5.549528623305378e-7,2.685182494133187e-7,-1.2914859182089345e-9,0.0,0.0,0.0,2.936904195394163e-7,-5.362765023164684e-7,2.691804468213377e-7,5.229362614045385e-7,1.214893217138524e-7,3.68344103662821e-7,0.0,0.0,0.0,0.0,0.0,0.0,0.5957686901092529,-0.018923696130514146,0.210589200258255,1.0025907754898072,-0.13775762915611268,-0.0028545460663735868,-0.02064414694905281,-0.000006081602350604953,0.5957680940628052,-0.018925389274954797,0.21061649918556214,1.0025908946990967,-0.1377776861190796,-0.002855474827811122,-0.0206453874707222,-2.002984800589247e-12,3.415094056435919e-7,-1.1383647802176711e-7,0.3982909321784973,0.30049070715904238,-0.030618805438280107,0.9997979998588562,0.03679788112640381,-0.0025310651399195196,0.0003060820163227618,6.78518156441957e-15,-4.55345883665359e-7,0.398291677236557,0.3004913926124573,-0.030611246824264528,0.9997984170913696,0.03679078817367554,-0.0025303007569164039,0.0003055269189644605,-0.6851787567138672,0.45670729875564577,0.6459015607833862,0.645901620388031,0.6689663529396057,-0.4002758264541626,0.8113421201705933,0.8113429546356201,0.6677030324935913,-0.6235257387161255,0.8111323714256287,0.81113201379776,0.6683899164199829,-0.569826602935791,0.8116428256034851,0.8116353750228882,0.6692385077476502,-0.44004642963409426,0.808272123336792,0.8082727789878845,-0.684016764163971,0.4576999545097351,0.6457741260528565,0.6457732319831848,0.6689646244049072,-0.40025004744529726,0.8113406300544739,0.8113411664962769,0.6677078008651733,-0.623522937297821,0.8111324310302734,0.811129093170166,0.6683884263038635,-0.5698763728141785,0.8116430640220642,0.8116341829299927,0.6692467331886292,-0.44011595845222475,0.8082780241966248,0.8082770705223084]}";
		private static KeyValuePair<bool, HumanPose> s_UniVRM_Tpose = default;
		private HumanPose UniVRM_Tpose
		{
			get
			{
				if (!s_UniVRM_Tpose.Key)
				{
					var data = JsonUtility.FromJson<HumanPose>(s_UniVRM_Tpose_json);
					s_UniVRM_Tpose = new KeyValuePair<bool, HumanPose>(true, data);
				}
				return s_UniVRM_Tpose.Value;
			}
		}

        [ContextMenu("Force T-Pose")]
		public void ForceTPose()
        {
			var aniEnable = animator.enabled;
			animator.enabled = false;// Disable animator to prevent any animation from interfering with T-Pose

#if USE_UNIVRM
			// UniVRM
			HumanPoseTransfer.SetTPose(animator.avatar, transform);
			//var humanPoseClip = Resources.Load<HumanPoseClip>(HumanPoseClip.TPoseResourcePath);
			//var pose = humanPoseClip.GetPose();
			//var json = JsonUtility.ToJson(pose); // copy from UniVRM T-Pose
			//HumanPoseTransfer.SetPose(animator.avatar, transform, pose);
#elif USE_ADOBE_TPOSE
			// Adobe Mixamo T - Pose,
			if (animator.runtimeAnimatorController == null)
			{
				Debug.LogError("Require runtime animator controller", animator);
				return;
			}
			animator.playableGraph.Evaluate(0);
#else
			// serialize UniVRM T-Pose to JSON.
			// work without UniVRM package.
			var pose = UniVRM_Tpose;
			var handler = new HumanPoseHandler(animator.avatar, transform);
			handler.SetHumanPose(ref pose);
#endif

			if (m_Pivot == null)
            {
                m_Pivot = new GameObject("TPose").transform;
                m_Pivot.SetParent(transform, false);
                m_Pivot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			}

            if (m_BoneRefs == null || m_BoneRefs.Length == 0)
            {
				m_BoneRefs = new Transform[(int)HumanBodyBones.LastBone]; // Exclude LastBone
            }


            var hbones = typeof(HumanBodyBones).GetEnumValues();
			for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
            {
                var boneTransform = animator.GetBoneTransform(b);
                if (boneTransform == null)
                    continue;

                var i = (int)b;
                GameObject clone = new GameObject(b.ToString());
                clone.transform.SetParent(m_Pivot, false);
                clone.transform.SetPositionAndRotation(boneTransform.position, boneTransform.rotation);
				m_BoneRefs[i] = clone.transform;
			}

			for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
			{
				var i = (int)b;
				var child = m_BoneRefs[i];
				if (child == null)
					continue;
				if (!s_ParentBoneDict.TryGetValue(b, out var pEnum))
					continue;

				var parent = m_BoneRefs[(int)pEnum];
				if (parent == null)
					continue;

				child.SetParent(parent, true);
			}

			animator.enabled = aniEnable; // Restore animator state
		}

		public bool TryGetTPose(HumanBodyBones boneTag, out Transform bone)
		{
			var i = (int)boneTag;
			if (i < 0 || i >= (int)HumanBodyBones.LastBone)
			{
				bone = default;
				return false;
			}
			bone = m_BoneRefs[i];
			return bone != null;
		}

		private void OnDrawGizmos()
		{
			if (m_BoneRefs == null || m_BoneRefs.Length != (int)HumanBodyBones.LastBone)
				return;
			var drawTposeBone = m_Debug.gizmos.HasFlag(eDebugDraw.TPoseBone);
			var drawBone = m_Debug.gizmos.HasFlag(eDebugDraw.Bone);
			var drawRotation = m_Debug.gizmos.HasFlag(eDebugDraw.Rotation);

			if (drawTposeBone)
			{
				using (var col = new ColorScope(Color.blue))
				{
					for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
					{
						var i = (int)b;
						var child = m_BoneRefs[i];
						if (child == null)
							continue;

						if (!s_ParentBoneDict.TryGetValue(b, out var pEnum))
							continue;
						var parent = m_BoneRefs[(int)pEnum];
						if (parent == null)
							continue;

						Gizmos.DrawLine(parent.position, child.position);

						if (drawRotation)
						{
							GizmosExtend.DrawTransform(child, false, 0.2f);
						}
					}
				}
			}

			if (drawBone)
			{
				using (var col = new ColorScope(m_Debug.boneColor))
				{
					for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
					{
						var i = (int)b;
						var child = animator.GetBoneTransform(b);
						if (child == null)
							continue;

						if (!s_ParentBoneDict.TryGetValue(b, out var pEnum))
							continue;
						var parent = animator.GetBoneTransform(pEnum);
						if (parent == null)
							continue;

						Gizmos.DrawLine(parent.position, child.position);

						if (drawRotation)
						{
							GizmosExtend.DrawTransform(child, false, 0.2f);
						}
					}
				}
			}
		}

		private void Update()
		{
			FetchTargets();
			if (!m_LateUpdate)
				ApplyAnimationsByWeights();
		}

		private void LateUpdate()
		{
			if (m_LateUpdate)
				ApplyAnimationsByWeights();
		}

		private const int BONE_COUNT = (int)HumanBodyBones.LastBone;
		private class PoseSnapshot
		{
			public Quaternion[] rotations;
			public Vector3 hipOffset;

			public void Snapshot(Animator target)
			{
				if (target == null)
					throw new System.NullReferenceException("Animator is null or not set.");
				if (!target.isHuman)
					throw new System.InvalidOperationException("Animator is not humanoid.");
				this.rotations = new Quaternion[BONE_COUNT]; // Exclude LastBone
				this.hipOffset = Vector3.zero;
				for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
				{
					var bone = target.GetBoneTransform(b);
					if (bone == null)
						continue;
					var i = (int)b;
					this.rotations[i] = bone.rotation;

					if (b == HumanBodyBones.Hips)
					{
						// We need to store the hip offset in local space of the pivot, so we can apply it later.
						//var worldOffset = bone.position - target.transform.position;
						//hipOffset = Quaternion.Inverse(target.transform.rotation) * worldOffset; // Convert to local space
						hipOffset = bone.localPosition; // cheap.
					}
				}
			}
		}

		private class PoseInfo : PoseSnapshot
		{
			public readonly GxRetargeting target, self;

			public PoseInfo(GxRetargeting target, GxRetargeting self)
			{
				if (target == null || target.animator == null)
					throw new System.NullReferenceException("Target animator is null or not set.");

				this.target = target;
				this.self = self;
				this.rotations = new Quaternion[(int)HumanBodyBones.LastBone]; // Exclude LastBone
				this.hipOffset = Vector3.zero;
			}

			public void Evaluate()
			{
				if (target == null)
					return;
				// optimize, calculate once and apply to all bones
				Transform fromRoot			= this.target.transform;
				Transform toRoot			= this.self.transform;
				Transform fromPivot			= this.target.pivot;
				Transform toPivot			= this.self.pivot;
				Quaternion revertFromPivot	= Quaternion.Inverse(fromPivot.rotation);
				Quaternion revertToPivot	= Quaternion.Inverse(toPivot.rotation);

				for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
				{
					if (s_Ignore.Contains(b))
						continue;
					// Prepare related data for re-targeting
					if (!self.TryGetTPose(b, out var toTpose))
						continue;
					if (!target.TryGetTPose(b, out var fromTpose))
						continue;

					Transform fromCurrent	= target.animator.GetBoneTransform(b);
					Transform toCurrent		= self.animator.GetBoneTransform(b);
					Debug.Assert(fromCurrent != null && toCurrent != null, "Bone missing at runtime", self);

					// Assume both T-Pose will not changed at runtime.
					// Calculate the bone rotation in local space of the pivot
					// find out the delta rotation from clone target and reapply the rotation to the current bone

					// inverse world rotation, therefor we can calculate the delta rotation in local space of the pivot
					Quaternion fromLocalTPose	= revertFromPivot * fromTpose.rotation;
					Quaternion toLocalTPose		= revertToPivot * toTpose.rotation;
					Quaternion modelDiff		= Quaternion.Inverse(fromLocalTPose) * toLocalTPose;

					// calculate thee clone target bone rotation in local space of the pivot
					var sourceCurrentLocal		= revertFromPivot * fromCurrent.rotation;

					// Apply the delta rotation between 2 model, to the current bone in local space of the pivot
					var i = (int)b;
					this.rotations[i]			= toPivot.rotation * sourceCurrentLocal * modelDiff;

					if (b == HumanBodyBones.Hips)
					{
						// Hips is the root bone, we need to apply the position as well.
						var fromLegSqr	= (fromTpose.position - fromRoot.position).sqrMagnitude;
						var toLegSqr	= (toTpose.position - toRoot.position).sqrMagnitude;
						var ratio		= fromLegSqr <= 0f ? 0f : toLegSqr / fromLegSqr;

						// apply hip movement, based on the ratio of leg length between models.
						var localHipOffset = revertFromPivot * (fromCurrent.position - fromRoot.position);
						this.hipOffset	= toPivot.rotation * (localHipOffset * ratio);
					}
				}
			}
		}
		private Dictionary<GxRetargeting, PoseInfo> m_PoseDict = new Dictionary<GxRetargeting, PoseInfo>();
		private PoseSnapshot m_LastPose;
		private void FetchTargets()
		{
			if (targetCount == 0)
				return;

			m_PoseDict.Clear();
			foreach (var t in targets)
			{
				var target = t.GetTarget();
				if (target == null || target.animator == null)
					continue;
				if (!m_PoseDict.TryGetValue(target, out var poseInfo))
				{
					poseInfo = new PoseInfo(target, this);
					m_PoseDict.Add(target, poseInfo);
				}
				poseInfo.Evaluate();
			}
		}

		public void ApplyAnimationsByWeights()
		{
			if (targetCount == 0)
				return;

			var totalWeight = 0f;
			foreach (var t in targets)
			{
			   	var weight = t.GetWeight01();
				if (weight <= 0f)
					continue;
				totalWeight += weight;
			}
			if (totalWeight <= float.Epsilon)
			{
				// no animation to apply.
				return;
			}

			if (m_LastPose == null)
			{
				m_LastPose = new PoseSnapshot();
				m_LastPose.Snapshot(animator);
			}

			var boneCnt = (int)HumanBodyBones.LastBone;
			List<Quaternion> cacheRots = new List<Quaternion>(boneCnt);
			List<Vector4> cachePos = new List<Vector4>(targetCount);
			List<float> cacheWeights = new List<float>(boneCnt);

			var hipOffset = Vector3.zero;

			for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
			{
				if (s_Ignore.Contains(b))
					continue;

				cachePos.Clear();
				cacheRots.Clear();
				cacheWeights.Clear();
				
				// fetch all target rotations and weights for this bone
				foreach (var t in targets)
				{
					var weight = t.GetWeight01();
					if (weight <= 0f)
						continue;
					var target = t.GetTarget();
					if (target == null || target.animator == null)
						continue;
					if (!m_PoseDict.TryGetValue(target, out var poseInfo))
						continue;
					cacheRots.Add(poseInfo.rotations[(int)b]);
					cacheWeights.Add(weight);
					if (!m_RemoveHipMotion && b == HumanBodyBones.Hips)
					{
						var v4 = (Vector4)poseInfo.hipOffset;
						v4.w = weight; // Store weight in w component
						cachePos.Add(v4);
					}
				}

				// Calculate the final rotation
				var finalRotation = QuaternionExtend.WeightedAverage(cacheRots.ToArray(), cacheWeights.ToArray());
				var bone = animator.GetBoneTransform(b);
				if (m_SmoothPose)
				{
					var lastRot = m_LastPose.rotations[(int)b];
					// If the rotation is flipped, we need to slerp unclamped to avoid flipping
					if (Quaternion.Dot(finalRotation, lastRot) < 0f)
					{
						finalRotation = Quaternion.Slerp(lastRot, finalRotation, 0.5f);
					}
				}
				bone.rotation = finalRotation;

				if (!m_RemoveHipMotion && b == HumanBodyBones.Hips)
				{
					var finalPosOffset = Vector3Extend.Centroid(cachePos.ToArray());
					bone.position = transform.position + finalPosOffset;
				}
			}

			m_LastPose.Snapshot(animator);
			cachePos.Clear();
			cacheRots.Clear();
			cacheWeights.Clear();
			hipOffset = default;
		}

		public void AddTarget(IRetarget data)
		{
			m_TargetsList.Add(data);
		}

		public void RemoveTarget(IRetarget data)
		{
			m_TargetsList.Remove(data);
		}

		private static readonly Dictionary<HumanBodyBones, HumanBodyBones> s_ParentBoneDict = new Dictionary<HumanBodyBones, HumanBodyBones>
		{
			{ HumanBodyBones.Head,          HumanBodyBones.Neck },
			{ HumanBodyBones.Neck,          HumanBodyBones.UpperChest },
			{ HumanBodyBones.UpperChest,    HumanBodyBones.Chest },
			{ HumanBodyBones.Chest,         HumanBodyBones.Spine },
			{ HumanBodyBones.Spine,         HumanBodyBones.Hips },
			{ HumanBodyBones.LeftUpperArm,  HumanBodyBones.Chest },
			{ HumanBodyBones.RightUpperArm, HumanBodyBones.Chest },
			{ HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftUpperArm },
			{ HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm },
			{ HumanBodyBones.LeftHand,      HumanBodyBones.LeftLowerArm },
			{ HumanBodyBones.RightHand,     HumanBodyBones.RightLowerArm },
			{ HumanBodyBones.LeftUpperLeg,  HumanBodyBones.Hips },
			{ HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips },
			{ HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftUpperLeg },
			{ HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg },
			{ HumanBodyBones.LeftFoot,      HumanBodyBones.LeftLowerLeg },
			{ HumanBodyBones.RightFoot,     HumanBodyBones.RightLowerLeg },
			{ HumanBodyBones.LeftToes,      HumanBodyBones.LeftFoot },
			{ HumanBodyBones.RightToes,     HumanBodyBones.RightFoot },
			{ HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate },
			{ HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal },
			{ HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate },
			{ HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal },
			{ HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate },
			{ HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal },
			{ HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate },
			{ HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal },
			{ HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate },
			{ HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal },
			{ HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftHand },
			{ HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate },
			{ HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal },
			{ HumanBodyBones.RightThumbProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate },
			{ HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal },
			{ HumanBodyBones.RightIndexProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate },
			{ HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal },
			{ HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate },
			{ HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal },
			{ HumanBodyBones.RightRingProximal, HumanBodyBones.RightHand },
			{ HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate },
			{ HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal },
			{ HumanBodyBones.RightLittleProximal, HumanBodyBones.RightHand }
		};

		private static readonly HashSet<HumanBodyBones> s_Ignore = new HashSet<HumanBodyBones>
		{
			HumanBodyBones.LeftEye,
			HumanBodyBones.RightEye,
			HumanBodyBones.Jaw,
			HumanBodyBones.LastBone,
		};
	}
}