using Kit2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[System.Obsolete("use TShirtCtrlV2 instead")]
    public class TShirtCtrl : MonoBehaviour
    {
		[System.Serializable]
		public class Setting
		{
			public Transform Chest;
			public Transform LUA, LLA, LF, LB, LT, LD;
			public Transform shirtBL, shirtFL;

			public Transform RUA, RLA, RF, RB, RT, RD;
			public Transform shirtBR, shirtFR;

			public Vector3 flcp, frcp, flap, frap, blcp, brcp, blap, brap;
			public Vector3 flcr, frcr, flar, frar, blcr, brcr, blar, brar;
			
			[Range(0f, 10f)] public float m_PosLerpSpeed = 1f;
			[Range(0f, 10f)] public float m_RotLerpSpeed = 1f;

			public void CalcOffset()
			{
				if (Chest == null)
					return;
				if (LUA == null || LF == null || LB == null || LT == null || LD == null)
					return;
				if (RUA == null || RF == null || RB == null || RT == null || RD == null)
					return;
				if (shirtBL == null || shirtFL == null || shirtBR == null || shirtFR == null)
					return;

				var chestRotInv = Chest.rotation.Inverse();
				var luRotInv = LUA.rotation.Inverse();
				var ruRotInv = RUA.rotation.Inverse();

				flcp = Chest.InverseTransformPoint(shirtFL.position);
				flcr = (chestRotInv * shirtFL.rotation).eulerAngles;

				frcp = Chest.InverseTransformPoint(shirtFR.position);
				frcr = (chestRotInv * shirtFR.rotation).eulerAngles;

				flap = LUA.InverseTransformPoint(shirtFL.position);
				flar = (luRotInv * shirtFL.rotation).eulerAngles;

				frap = RUA.InverseTransformPoint(shirtFR.position);
				frar = (ruRotInv * shirtFR.rotation).eulerAngles;

				blcp = Chest.InverseTransformPoint(shirtBL.position);
				blcr = (chestRotInv * shirtBL.rotation).eulerAngles;

				brcp = Chest.InverseTransformPoint(shirtBR.position);
				brcr = (chestRotInv * shirtBR.rotation).eulerAngles;

				blap = LUA.InverseTransformPoint(shirtBL.position);
				blar = (luRotInv * shirtBL.rotation).eulerAngles;

				brap = RUA.InverseTransformPoint(shirtBR.position);
				brar = (ruRotInv * shirtBR.rotation).eulerAngles;
			}
		}
		public Setting m_Rig = null;
		public float m_ArmRadius = 0.1f;

		public Vector3 lf => m_Rig.LF.position;
		public Vector3 lb => m_Rig.LB.position;
		public Vector3 lt => m_Rig.LT.position;
		public Vector3 ld => m_Rig.LD.position;
		public Vector3 rf => m_Rig.RF.position;
		public Vector3 rb => m_Rig.RB.position;
		public Vector3 rt => m_Rig.RT.position;
		public Vector3 rd => m_Rig.RD.position;

		public struct Ring
		{
			public Vector3		position;
			public Quaternion	rotation;
			public float		radius;
			public Vector3 up		=> rotation * Vector3.up;
			public Vector3 forward	=> rotation * Vector3.forward;
			public Vector3 right	=> rotation * Vector3.right;
			public Vector3 back		=> rotation * Vector3.back;
			public Ring(Vector3 position, Vector3 fwd, Vector3 up, float radius)
			{
				this.position	= position;
				this.rotation	= Quaternion.LookRotation(fwd, up);
				this.radius		= radius;
			}
			public Ring(ReadOnlySpan<Vector3> vector3s)
			{
				var anchor = Vector3Extend.Centroid(vector3s);
				var up = Vector3.Cross(vector3s[1] - vector3s[0], vector3s[3] - vector3s[0]).normalized;
				var v = vector3s[0] - anchor;
				var fwd = v.normalized;
				var radius = v.magnitude;
				this.position = anchor;
				this.rotation = Quaternion.LookRotation(fwd, up);
				this.radius = radius;
			}
		}

		public void GetSleevesRing(bool isLeft, out Ring sleeves)
		{
			var arr		= isLeft ? new Vector3[] { lf, lt, lb, ld, } : new Vector3[] { rf, rt, rb, rd, };
			sleeves		= new Ring(arr);
		}
		public void GetArmRing(bool isLeft, out Ring arm)
		{
			var pos = isLeft ? m_Rig.LLA.position : m_Rig.RLA.position;
			var v = isLeft ? m_Rig.LUA.position - m_Rig.LLA.position : m_Rig.RUA.position - m_Rig.RLA.position;
			var up = v.normalized;
			arm = new Ring(pos, m_Rig.Chest.forward, up, m_ArmRadius);
		}

		private void OnDrawGizmos()
		{
			if (IsInvalid())
				return;

			GetSleevesRing(true, out var ls);
			GetSleevesRing(false, out var rs);
			using (new ColorScope(Color.blue))
			{
				Gizmos.DrawRay(ls.position, ls.forward * ls.radius * 2f);
				Gizmos.DrawRay(rs.position, rs.forward * ls.radius * 2f);
				GizmosExtend.DrawCircle(ls.position, ls.up, radius: ls.radius * 1.5f);
				GizmosExtend.DrawCircle(rs.position, ls.up, radius: ls.radius * 1.5f);
			}

			GetArmRing(true, out var lArm);
			GetArmRing(false, out var rArm);
			using (new ColorScope(Color.magenta))
			{
				GizmosExtend.DrawCircle(lArm.position, lArm.up, radius: lArm.radius);
				GizmosExtend.DrawCircle(rArm.position, rArm.up, radius: rArm.radius);
			}

			var spine = m_Rig.Chest.position;
			using (new ColorScope(Color.magenta))
			{
				Gizmos.DrawLine(spine, m_Rig.shirtFL.position);
				Gizmos.DrawLine(m_Rig.shirtFL.position, m_Rig.LUA.position);

				Gizmos.DrawLine(spine, m_Rig.shirtBL.position);
				Gizmos.DrawLine(m_Rig.shirtBL.position, m_Rig.LUA.position);

				Gizmos.DrawLine(spine, m_Rig.shirtFR.position);
				Gizmos.DrawLine(m_Rig.shirtFR.position, m_Rig.RUA.position);

				Gizmos.DrawLine(spine, m_Rig.shirtBR.position);
				Gizmos.DrawLine(m_Rig.shirtBR.position, m_Rig.RUA.position);
			}
		}

		private bool IsInvalid()
		{
			return
				m_Rig.Chest == null ||
				m_Rig.LUA == null || m_Rig.LF == null || m_Rig.LB == null || m_Rig.LT == null || m_Rig.LD == null ||
				m_Rig.RUA == null || m_Rig.RF == null || m_Rig.RB == null || m_Rig.RT == null || m_Rig.RD == null ||
				m_Rig.shirtBL == null || m_Rig.shirtFL == null || m_Rig.shirtBR == null || m_Rig.shirtFR == null;
		}

		#region MonoBehaviour
		private void OnValidate()
		{
			if (IsInvalid())
				return;
			m_Rig.CalcOffset();
		}

		private void Awake()
		{
			if (IsInvalid())
			{
				Debug.LogError("Invalid Setup.");
				this.enabled = false;
				return;
			}
			m_Rig.CalcOffset();
		}

		private void Update()
		{
			MaintainBonePos(m_Rig.shirtFL, m_Rig.Chest, m_Rig.LUA, m_Rig.flcp, m_Rig.flcr, m_Rig.flap, m_Rig.flar);
			MaintainBonePos(m_Rig.shirtBL, m_Rig.Chest, m_Rig.LUA, m_Rig.blcp, m_Rig.blcr, m_Rig.blap, m_Rig.blar);
			MaintainBonePos(m_Rig.shirtFR, m_Rig.Chest, m_Rig.RUA, m_Rig.frcp, m_Rig.frcr, m_Rig.frap, m_Rig.frar);
			MaintainBonePos(m_Rig.shirtBR, m_Rig.Chest, m_Rig.RUA, m_Rig.brcp, m_Rig.brcr, m_Rig.brap, m_Rig.brar);
		}

		private void MaintainBonePos(Transform shirt, Transform spine, Transform arm, 
			Vector3 cpOffset, Vector3 crOffset,
			Vector3 apOffset, Vector3 arOffset)
		{
			if (spine == null || arm == null || shirt == null)
				return;

			var p0 = spine.TransformPoint(cpOffset);
			var p1 = arm.TransformPoint(apOffset);

			var r0 = spine.rotation * Quaternion.Euler(crOffset);
			var r1 = arm.rotation * Quaternion.Euler(arOffset);

			var pt = m_Rig.m_PosLerpSpeed * Time.deltaTime;
			var rt = m_Rig.m_RotLerpSpeed * Time.deltaTime;

			var p = Vector3.Lerp(p0, p1, 0.5f);
			var r = Quaternion.Slerp(r0, r1, 0.5f);
			shirt.SetPositionAndRotation(p, r);
		}
		#endregion MonoBehaviour

	}
}