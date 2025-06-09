using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
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

		[System.Serializable]		
		public struct TargetInfo
		{
			[Range(0f, 1f)] public float weight;
			public GxRetargeting target;
		}
		[SerializeField] TargetInfo[] m_Targets = new TargetInfo[0];

		[SerializeField] Transform m_Pivot;
		public Transform pivot => m_Pivot;
		[SerializeField] bool m_RemoveHipMotion = false;
		[SerializeField] Transform[] m_BoneRefs;

		[SerializeField] bool m_LateUpdate = false;

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

		private void Reset()
		{
			m_Animator = GetComponent<Animator>();
		}

        [ContextMenu("Force T-Pose")]
		public void ForceTPose()
        {
            if (Application.isPlaying)
                return;

#if UNITY_EDITOR
			animator.playableGraph.Evaluate(0);

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
#endif
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

		private class PoseInfo
		{
			public readonly GxRetargeting target, self;
			public Quaternion[] rotations;
			public Vector3 hipOffset;

			public PoseInfo(GxRetargeting target, GxRetargeting self)
			{
				if (target == null || target.animator == null)
					throw new System.NullReferenceException("Target animator is null or not set.");

				this.target = target;
				this.self = self;
				this.rotations = new Quaternion[(int)HumanBodyBones.LastBone]; // Exclude LastBone
				this.hipOffset = Vector3.zero;
			}

			public void Snapshot()
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
		private void FetchTargets()
		{
			if (m_Targets == null || m_Targets.Length == 0)
				return;

			m_PoseDict.Clear();
			for (int i = 0; i < m_Targets.Length; ++i)
			{
				var target = m_Targets[i].target;
				if (target == null || target.animator == null)
					continue;
				if (!m_PoseDict.TryGetValue(target, out var poseInfo))
				{
					poseInfo = new PoseInfo(target, this);
					m_PoseDict.Add(target, poseInfo);
				}
				poseInfo.Snapshot();
			}
			// CloneInfo
		}

		public void ApplyAnimationsByWeights()
		{
			if (m_Targets == null || m_Targets.Length == 0)
				return;

			var boneCnt = (int)HumanBodyBones.LastBone;
			List<Quaternion> cacheRots = new List<Quaternion>(boneCnt);
			List<Vector4> cachePos = new List<Vector4>(m_Targets.Length);
			List<float> cacheWeights = new List<float>(boneCnt);

			var hipOffset = Vector3.zero;

			for (var b = HumanBodyBones.Hips; b < HumanBodyBones.LastBone; ++b)
			{
				if (s_Ignore.Contains(b))
					continue;

				cachePos.Clear();
				cacheRots.Clear();
				cacheWeights.Clear();
				for (int t = 0; t < m_Targets.Length; ++t)
				{
					var info = m_Targets[t];
					var target = info.target;
					if (target == null || target.animator == null)
						continue;

					if (info.weight <= 0f)
						continue;
					var weight = Mathf.Clamp01(info.weight);

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
				} // End for

				// Calculate the final rotation
				var finalRotation = QuaternionExtend.WeightedAverage(cacheRots.ToArray(), cacheWeights.ToArray());
				var bone = animator.GetBoneTransform(b);
				bone.rotation = finalRotation;

				if (!m_RemoveHipMotion && b == HumanBodyBones.Hips)
				{
					var finalPosOffset = Vector3Extend.Centroid(cachePos.ToArray());
					bone.position = transform.position + finalPosOffset;
				}
			}
			cachePos.Clear();
			cacheRots.Clear();
			cacheWeights.Clear();
			hipOffset = default;
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