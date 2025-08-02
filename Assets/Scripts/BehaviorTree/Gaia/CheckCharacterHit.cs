using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DesktopWizard;
using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Check Character Hit")]
	[TaskDescription("Check Character Hit.")]
	public class CheckCharacterHit : CharacterCondition
    {
        [Header("Ray Position - Input (one of forward)")]
		[SerializeField] SharedVector2Int m_OS_Pos;
		[SerializeField] SharedVector2 m_Monitor_Pos;
        [SerializeField] SharedVector2 m_Form_Pos;
		[SerializeField] bool m_UseCursorPos = false;
		[SerializeField] SharedBool m_NoEventAsFailure = false;

		private class ClickInfo
		{
			public Vector2Int OS_Pos;
			public Vector2 Monitor_Pos;
			public Vector2 Form_Pos;
			public PointerEventData evt;

			public bool valid = false;
			public ClickInfo(PointerEventData evt, DwCamera c)
			{
				this.evt = evt;

				this.OS_Pos = c.GetMousePosInOSSpace();
				var osV3f = new Vector3(OS_Pos.x, OS_Pos.y, 0f);
				this.Monitor_Pos = c.MatrixOSToMonitor().MultiplyPoint3x4(osV3f);
				this.Form_Pos = c.MatrixOSToForm().MultiplyPoint3x4(osV3f);
				this.valid = true;
			}

			public void Consume()
			{
				this.valid = false;
			}
		}
		private ClickInfo m_ClickInfo;

		[Header("Raycast - setting")]
		[SerializeField] SharedFloat m_Radius = 0f;
		[SerializeField] SharedFloat m_MaxDistance = 1000f;
		[SerializeField] LayerMask m_LayerMask = -1; // Default to all layers
		[SerializeField] QueryTriggerInteraction m_QueryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

		[Header("Character Hit - Output")]
        [SerializeField] SharedCollider m_HitCollider;
		[SerializeField] SharedGameObject m_HitGameObject;
		[SerializeField] SharedVector3 m_HitPoint;
		[SerializeField] SharedFloat m_HitDistance;

		[SerializeField] SharedBool m_HitLeftArm;
		[SerializeField] SharedBool m_HitRightArm;
		[SerializeField] SharedBool m_HitHead;
		[SerializeField] SharedBool m_HitChest;
		[SerializeField] SharedBool m_HitHips;
		[SerializeField] SharedBool m_HitLeftLeg;
		[SerializeField] SharedBool m_HitRightLeg;

		protected override eState OnModelViewUpdate()
        {
            if (ModelView == null || ModelView.dwCamera == null)
                return eState.Failure;
            if (Character == null)
                return eState.Failure;

			eState NoRayResult()
			{
				return m_NoEventAsFailure.Value ?
					eState.Failure :
					eState.Running;
			}

            var c = ModelView.dwCamera;
			// Convert click event into ray.
			// notes: ray info may not update for some cases (!m_UseCursorPos)
			if (!TryGetRay(out Ray ray))
				return NoRayResult();
			const bool DEBUG_DEPTH = false;
			const float DEBUG_DURATION = 5f;
			var maxDistance = m_MaxDistance.IsNone ? float.MaxValue : m_MaxDistance.Value;
			var radius = m_Radius.IsNone ? 0f : m_Radius.Value;
			if (radius < 0.0001f)
			{
				if (!Physics.Raycast(ray, out var raycastHit, maxDistance, m_LayerMask, m_QueryTriggerInteraction))
				{
					DebugExtend.DrawRay(ray.origin, ray.direction * maxDistance, Color.magenta, DEBUG_DURATION, DEBUG_DEPTH);
					return NoRayResult();
				}
				DebugExtend.DrawRay(ray.origin, ray.direction * raycastHit.distance, Color.green, DEBUG_DURATION, DEBUG_DEPTH);
				CheckHitParts(raycastHit);
				return eState.Success;
			}
			else
			{
				if (!Physics.SphereCast(ray, radius, out var raycastHit, maxDistance, m_LayerMask, m_QueryTriggerInteraction))
				{
					DebugExtend.DrawCylinder(ray.origin, ray.origin + ray.direction * maxDistance, Color.magenta, radius, DEBUG_DURATION, DEBUG_DEPTH);
					return NoRayResult();
				}
				DebugExtend.DrawCylinder(ray.origin, ray.origin + ray.direction * raycastHit.distance, Color.green, radius, DEBUG_DURATION, DEBUG_DEPTH);
				CheckHitParts(raycastHit);
				return eState.Success;
			}
        }

		private void CheckHitParts(RaycastHit raycastHit)
		{
			m_HitCollider.SetValue(raycastHit.collider);
			m_HitGameObject.SetValue(raycastHit.collider?.gameObject);
			m_HitPoint.SetValue(raycastHit.point);
			m_HitDistance.SetValue(raycastHit.distance);

			m_HitLeftArm.SetValue(IsLeftArm(raycastHit.transform, out _));
			m_HitRightArm.SetValue(IsRightArm(raycastHit.transform, out _));
			m_HitHead.SetValue(IsHead(raycastHit.transform, out _));
			m_HitChest.SetValue(IsChest(raycastHit.transform, out _));
			m_HitHips.SetValue(IsHips(raycastHit.transform, out _));
			m_HitLeftLeg.SetValue(IsLeftLeg(raycastHit.transform, out _));
			m_HitRightLeg.SetValue(IsRightLeg(raycastHit.transform, out _));
		}

		private bool TryGetRay(out Ray ray)
        {
			var c = ModelView?.dwCamera;
			if (c == null || c.linkCamera == null)
			{
				ray = default;
				return false;
			}

			if (m_UseCursorPos)
			{
				if (m_ClickInfo == null || !m_ClickInfo.valid)
				{
					ray = default;
					return false;
				}

				// Use the current cursor position.
				var v2i = m_ClickInfo.OS_Pos;
				var fromPos = m_ClickInfo.Form_Pos;
				ray = c.linkCamera.ScreenPointToRay((Vector3)fromPos);
				m_ClickInfo.Consume();
				return true;
			}
			else if (!m_Form_Pos.IsNone)
			{
				var fromPos = m_Form_Pos.Value;
				ray = c.linkCamera.ScreenPointToRay((Vector3)fromPos);
				return true;
			}
			else if (!m_Monitor_Pos.IsNone)
			{
				var monPos = m_Monitor_Pos.Value;
				var fromPos = c.MatrixMonitorToOS().MultiplyPoint3x4(new Vector3(monPos.x, monPos.y, 0f));
				ray = c.linkCamera.ScreenPointToRay((Vector3)fromPos);
				return true;
			}
			else if (!m_OS_Pos.IsNone)
			{
				var os = m_OS_Pos.Value;
				var fromPos = c.MatrixOSToForm().MultiplyPoint3x4(new Vector3(os.x, os.y, 0f));
				ray = c.linkCamera.ScreenPointToRay((Vector3)fromPos);
				return true;
			}
			// If no position is provided, we cannot determine the ray.
			Debug.LogWarning("GetCharacterHIt: If no position is provided, we cannot determine the ray.");
			ray = default;
			return false;
		}

		private void InternalReset()
		{
			m_HitCollider.SetValue(null);
			m_HitGameObject.SetValue(null);
			m_HitPoint.SetValue(Vector3.zero);
			m_HitDistance.SetValue(0f);

			m_HitLeftArm.SetValue(false);
			m_HitRightArm.SetValue(false);
			m_HitHead.SetValue(false);
			m_HitChest.SetValue(false);
			m_HitHips.SetValue(false);
			m_HitLeftLeg.SetValue(false);
			m_HitRightLeg.SetValue(false);
			m_ClickInfo = null;
		}

		public override void OnStart()
        {
            base.OnStart();
			// InternalReset();

			if (ModelView.dwCamera != null)
			{
				var c = ModelView.dwCamera;
				// c.EVENT_MouseDown += C_EVENT_MouseDown;
				c.EVENT_MouseUp += C_EVENT_MouseUp;
			}
		}
		public override void OnEnd()
		{
			base.OnEnd();
			if (ModelView.dwCamera != null)
			{
				var c = ModelView.dwCamera;
				// c.EVENT_MouseDown -= C_EVENT_MouseDown;
				c.EVENT_MouseUp -= C_EVENT_MouseUp;
			}
		}

		private void C_EVENT_MouseUp(UnityEngine.EventSystems.PointerEventData evt)
		{
			if (!m_UseCursorPos)
				return;
			m_ClickInfo = new ClickInfo(evt, dwCamera);
		}

		public static readonly HumanBodyBones[] LEFT_ARM = new HumanBodyBones[]
		{
			HumanBodyBones.LeftShoulder,
			HumanBodyBones.LeftUpperArm,
			HumanBodyBones.LeftLowerArm,
			HumanBodyBones.LeftHand,
			HumanBodyBones.LeftThumbProximal,
			HumanBodyBones.LeftThumbIntermediate,
			HumanBodyBones.LeftThumbDistal,
			HumanBodyBones.LeftIndexProximal,
			HumanBodyBones.LeftIndexIntermediate,
			HumanBodyBones.LeftIndexDistal,
			HumanBodyBones.LeftMiddleProximal,
			HumanBodyBones.LeftMiddleIntermediate,
			HumanBodyBones.LeftMiddleDistal,
			HumanBodyBones.LeftRingProximal,
			HumanBodyBones.LeftRingIntermediate,
			HumanBodyBones.LeftRingDistal,
			HumanBodyBones.LeftLittleProximal,
			HumanBodyBones.LeftLittleIntermediate,
			HumanBodyBones.LeftLittleDistal
		};

		public static readonly HumanBodyBones[] RIGHT_ARM = new HumanBodyBones[]
		{
			HumanBodyBones.RightShoulder,
			HumanBodyBones.RightUpperArm,
			HumanBodyBones.RightLowerArm,
			HumanBodyBones.RightHand,
			HumanBodyBones.RightThumbProximal,
			HumanBodyBones.RightThumbIntermediate,
			HumanBodyBones.RightThumbDistal,
			HumanBodyBones.RightIndexProximal,
			HumanBodyBones.RightIndexIntermediate,
			HumanBodyBones.RightIndexDistal,
			HumanBodyBones.RightMiddleProximal,
			HumanBodyBones.RightMiddleIntermediate,
			HumanBodyBones.RightMiddleDistal,
			HumanBodyBones.RightRingProximal,
			HumanBodyBones.RightRingIntermediate,
			HumanBodyBones.RightRingDistal,
			HumanBodyBones.RightLittleProximal,
			HumanBodyBones.RightLittleIntermediate,
			HumanBodyBones.RightLittleDistal
		};

		public static readonly HumanBodyBones[] HEADS = new HumanBodyBones[]
		{
			HumanBodyBones.Neck,
			HumanBodyBones.Head
		};

		public static readonly HumanBodyBones[] CHEST = new HumanBodyBones[]
		{
			HumanBodyBones.Chest,
			HumanBodyBones.UpperChest,
		};

		public static readonly HumanBodyBones[] HIPS = new HumanBodyBones[]
		{
			HumanBodyBones.Hips,
		};

		public static readonly HumanBodyBones[] LEFT_LEGS = new HumanBodyBones[]
		{
			HumanBodyBones.LeftUpperLeg,
			HumanBodyBones.LeftLowerLeg,
			HumanBodyBones.LeftFoot,
			HumanBodyBones.LeftToes
		};

		public static readonly HumanBodyBones[] RIGHT_LEGS = new HumanBodyBones[]
		{
			HumanBodyBones.RightUpperLeg,
			HumanBodyBones.RightLowerLeg,
			HumanBodyBones.RightFoot,
			HumanBodyBones.RightToes
		};

		private bool IsPartOf(in Transform tran, in HumanBodyBones[] bones, out HumanBodyBones closest)
		{
			closest = HumanBodyBones.LastBone;
			if (tran == null)
				return false;
			int i = bones.Length;
			while (i-- > 0)
			{
				var rf = Character?.animator?.GetBoneTransform(bones[i]);
				if (rf == null)
					continue;
				if (tran.IsChildOf(rf.transform))
				{
					closest = bones[i];
					return true;
				}
			}
			return false;
		}
		public bool IsLeftArm(Transform tr, out HumanBodyBones closest) => IsPartOf(tr, LEFT_ARM, out closest);
		public bool IsRightArm(Transform tr, out HumanBodyBones closest) => IsPartOf(tr, RIGHT_ARM, out closest);
		public bool IsHead(Transform tr, out HumanBodyBones closest) => IsPartOf(tr, HEADS, out closest);
		public bool IsChest(Transform tr, out HumanBodyBones closest) => IsPartOf(tr, CHEST, out closest);
		public bool IsHips(Transform tr, out HumanBodyBones closest) => IsPartOf(tr, HIPS, out closest);
		public bool IsLeftLeg(Transform tr, out HumanBodyBones closest) => IsPartOf(tr, LEFT_LEGS, out closest);
		public bool IsRightLeg(Transform tr, out HumanBodyBones closest) => IsPartOf(tr, RIGHT_LEGS, out closest);

	}
}