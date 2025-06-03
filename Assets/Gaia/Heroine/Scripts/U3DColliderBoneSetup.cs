using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
namespace Gaia
{
	public static class U3DColliderBoneSetup
	{
		private static Animator s_Animator;
		public static void ExecuteAutoSetup(Animator animator, float scale = 1f)
		{
			if (animator == null)
			{
				Debug.LogWarning("Animator not found, fail to setup collider bone.");
				return;
			}
			s_Animator = animator;
			CapsuleCollider cc;

			if (TrySetupCollider(HumanBodyBones.Head, "BBC-Head", out cc))
			{
				//var toPos = cc.transform.position + cc.transform.up * 0.27f;
				//CalcCapsule(cc, cc.transform, toPos, 0.2f, eDir.Y, Vector3.zero, false);
				cc.center = new Vector3(0, 0.07f, 0.02f) * scale;
				cc.radius = 0.09f * scale;
				cc.height = 0.23f * scale;
				cc.direction = 1;
			}

			if (TrySetupCollider(HumanBodyBones.Neck, "BBC-Neck", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.Head);
				CalcCapsule(scale, cc, cc.transform, next, 0.05f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.Chest, "BBC-Chest", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.UpperChest);
				CalcCapsule(scale, cc, cc.transform, next, 0.09f * scale, eDir.Y, new Vector3(0f, 0f, 0.01f) * scale, false);
			}

			if (TrySetupCollider(HumanBodyBones.Spine, "BBC-Spine", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.Chest);
				CalcCapsule(scale, cc, cc.transform, next, 0.07f * scale, eDir.Y, new Vector3(0f, 0f, 0.02f) * scale, false);
			}

			if (TrySetupCollider(HumanBodyBones.Hips, "BBC-Hips", out cc))
			{
				cc.center = new Vector3(0f, 0.04f, 0.02f) * scale;
				cc.radius = 0.06f * scale;
				cc.height = 0.25f * scale;
				cc.direction = 0;
			}

			if (TrySetupCollider(HumanBodyBones.RightShoulder, "BBC-RightShoulder", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
				CalcCapsule(scale, cc, cc.transform, next, 0.03f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.RightUpperArm, "BBC-RightUpperArm", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
				CalcCapsule(scale, cc, cc.transform, next, 0.03f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.RightLowerArm, "BBC-RightLowerArm", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.RightHand);
				CalcCapsule(scale, cc, cc.transform, next, 0.03f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.RightUpperLeg, "BBC-RightUpperLeg", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
				CalcCapsule(scale, cc, cc.transform, next, 0.05f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.RightLowerLeg, "BBC-RightLowerLeg", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.RightFoot);
				CalcCapsule(scale, cc, cc.transform, next, 0.05f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.LeftShoulder, "BBC-LeftShoulder", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
				CalcCapsule(scale, cc, cc.transform, next, 0.03f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.LeftUpperArm, "BBC-LeftUpperArm", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
				CalcCapsule(scale, cc, cc.transform, next, 0.03f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.LeftLowerArm, "BBC-LeftLowerArm", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.LeftHand);
				CalcCapsule(scale, cc, cc.transform, next, 0.03f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.LeftUpperLeg, "BBC-LeftUpperLeg", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
				CalcCapsule(scale, cc, cc.transform, next, 0.05f * scale, eDir.Y, Vector3.zero, false);
			}

			if (TrySetupCollider(HumanBodyBones.LeftLowerLeg, "BBC-LeftLowerLeg", out cc))
			{
				var next = s_Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
				CalcCapsule(scale, cc, cc.transform, next, 0.05f * scale, eDir.Y, Vector3.zero, false);
			}
		}

		#region Bone And IK Setup
		private enum eDir
		{
			X = 0,
			Y = 1,
			Z = 2,
		}

		private static void CalcCapsule(float scale, CapsuleCollider c, Transform a, Transform b, float radius,
			eDir direction, Vector3 center, bool inverse = false)
		{
			CalcCapsule(scale, c, a, b.position, radius, direction, center, inverse);
		}

		private static void CalcCapsule(float scale, CapsuleCollider c, Transform a, Vector3 b, float radius,
			eDir direction, Vector3 center, bool inverse = false)
		{
			c.transform.SetLocalPositionAndRotation(a.localPosition, a.localRotation);

			var v = b - a.position;
			var dir = v.normalized;
			var distance = v.magnitude * scale; // due to 100 scale up, fix on rig.

			c.radius = radius;
			c.direction = (int)direction;
			c.height = distance + radius + radius;
			var f = (inverse ? -1f : 1f) * distance * 0.5f;
			switch (c.direction)
			{
				case 0:
				{
					var offset = new Vector3(f, 0, 0);
					c.center = center + offset;
				}
				break;
				case 1:
				{
					var offset = new Vector3(0, f, 0);
					c.center = center + offset;
				}
				break;
				case 2:
				{
					var offset = new Vector3(0, 0, f);
					c.center = center + offset;
				}
				break;
			}

		}

		private static bool TrySetupCollider<T>(HumanBodyBones bone, string childName, out T cc)
			where T : Collider
		{
			cc = null;
			var target = s_Animator.GetBoneTransform(bone);
			if (target == null)
			{
				Debug.LogWarning($"Bone not found: {bone}, fail to setup collider");
				return false;
			}

#if false
			cc = target.GetOrAddComponent<T>();
#else
			var _tran = target.Find(childName);
			if (_tran == null)
			{
				var go = new GameObject(childName);
				_tran = go.transform;
				_tran.transform.SetParent(target);
				_tran.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
				_tran.localScale = Vector3.one;
			}
			cc = _tran.GetOrAddComponent<T>();
#endif
			cc.gameObject.layer = characterLayer;
			return true;
		}

		private static KeyValuePair<bool, LayerMask> m_CharLayer;
		private static LayerMask characterLayer
		{
			get
			{
				if (m_CharLayer.Key)
					return m_CharLayer.Value;
				m_CharLayer = new KeyValuePair<bool, LayerMask>(true, LayerMask.NameToLayer("Character"));
				return m_CharLayer.Value;
			}
		}
		#endregion Bone And IK Setup
	}
}