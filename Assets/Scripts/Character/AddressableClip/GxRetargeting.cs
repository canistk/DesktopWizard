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

        [SerializeField] Transform m_Pivot;
        [SerializeField] Transform[] m_BoneRefs;
		[SerializeField] Color m_GizmosColor = Color.blue;
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
			using (var col = new ColorScope(m_GizmosColor))
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
				}
			}
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
	}
}