using Kit2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    [RequireComponent(typeof(Animator))]
	public class BreatheHandle : MonoBehaviour
    {
		[SerializeField] Animator m_Animator;
		public Animator animator
		{
			get
			{
				if (m_Animator == null)
					m_Animator = GetComponent<Animator>();
				return m_Animator;
			}
		}

		[System.Serializable]
		public struct ProcedureInfo
		{
			/// <summary>The amplitude of the motion.</summary>
			public float amplitude;
			/// <summary>Time offset</summary>
			public float offset;

			public ProcedureInfo(float amplitude = 0f, float offset = 0f)
			{
				this.amplitude = amplitude;
				this.offset = offset;
			}
			public float GetSinDelta(float time)
				=> Mathf.Sin(time + offset) * amplitude;
		}

		[SerializeField] float m_BreatheSpeed = 1.0f;

		[Header("Spine-Chest")]
		[SerializeField] List<Transform> m_Spines;
		[SerializeField] AnimationCurve m_SpineCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		[SerializeField] Vector3 m_SpineRatio = new Vector3(1f, 0f, 0f);
		[SerializeField] ProcedureInfo m_SpineInfo = new ProcedureInfo(1f, 0.0f);

		[Header("optional")]
		[SerializeField] Transform m_ShoulderL;
		[SerializeField] Transform m_ShoulderR;
		[SerializeField] Vector3 m_ShoulderLRatio = new Vector3(1f, -.2f, 0f);
		[SerializeField] Vector3 m_ShoulderLOffset = Vector3.zero;
		[SerializeField] Vector3 m_ShoulderRRatio = new Vector3(1f,  .2f, 0f);
		[SerializeField] Vector3 m_ShoulderROffset = Vector3.zero;
		[SerializeField] ProcedureInfo m_ShoulderInfo = new ProcedureInfo(5f, 0.0f);
		[SerializeField] Transform m_UpperArmL;
		[SerializeField] Transform m_UpperArmR;
		[SerializeField] Vector3 m_UpperArmLRatio = new Vector3(1f, -.2f, 0f);
		[SerializeField] Vector3 m_UpperArmLOffset = Vector3.zero;
		[SerializeField] Vector3 m_UpperArmRRatio = new Vector3(1f,  .2f, 0f);
		[SerializeField] Vector3 m_UpperArmROffset = Vector3.zero;
		[SerializeField] ProcedureInfo m_UpperArmInfo = new ProcedureInfo(5f, 0.0f);

		[Header("Breast")]
		[SerializeField] Transform m_BreastL;
		[SerializeField] Transform m_BreastR;
		[SerializeField] AnimationCurve m_BreastCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		[SerializeField] Vector3 m_BreastLRatio = new Vector3(.8f, -0.2f, 0f);
		[SerializeField] Vector3 m_BreastRRatio = new Vector3(.8f, 0.2f, 0f);
		[SerializeField] float m_BreastAmplitude = 5f;
		[SerializeField] float m_BreastOffset = 0f;
		List<Transform> breastLArr, breastRArr;

		private class TransCache
		{
			public Transform transform;
			public Quaternion offset;
			public TransCache(Transform t, Transform root)
			{
				transform = t;
				offset = root.rotation.Inverse() * t.rotation;
			}
		}

		Dictionary<Transform, TransCache> m_TranDict;
		Dictionary<Transform, TransCache> TranDict
		{
			get
			{
				if (m_TranDict == null)
				{
					m_TranDict = new Dictionary<Transform, TransCache>();
					var root = animator.transform;
					foreach (var spine in m_Spines)
					{
						m_TranDict[spine] = new TransCache(spine, root);
					}
					if (m_ShoulderL) m_TranDict[m_ShoulderL] = new TransCache(m_ShoulderL, root);
					if (m_ShoulderR) m_TranDict[m_ShoulderR] = new TransCache(m_ShoulderR, root);
					if (m_UpperArmL) m_TranDict[m_UpperArmL] = new TransCache(m_UpperArmL, root);
					if (m_UpperArmR) m_TranDict[m_UpperArmR] = new TransCache(m_UpperArmR, root);
					breastLArr = new List<Transform>();
					if (m_BreastL)
					{
						var b = m_BreastL;
						while (b != null)
						{
							breastLArr.Add(b);
							m_TranDict.Add(b, new TransCache(b, root));
							b = b.childCount > 0 ? b.GetChild(0) : null;
						}
					}
					breastRArr = new List<Transform>();
					if (m_BreastR)
					{
						var b = m_BreastR;
						while (b != null)
						{
							breastRArr.Add(b);
							m_TranDict.Add(b, new TransCache(b, root));
							b = b.childCount > 0 ? b.GetChild(0) : null;
						}
					}
				}
				return m_TranDict;
			}
		}

		private void LateUpdate()
		{
			var root = animator.transform.rotation;
			var cnt = m_Spines.Count;
			var f = Time.timeSinceLevelLoad * m_BreatheSpeed;
			{
				var divisor	= cnt < 2 ? 1f : 1f / (cnt - 1f);
				for (int i = 0; i < cnt; ++i)
				{
					var spine	= m_Spines[i];
					var offset	= m_SpineCurve.Evaluate(i * divisor);
					var euler	= m_SpineRatio * m_SpineInfo.GetSinDelta(f + offset);
					var quat	= Quaternion.Euler(euler);
					spine.rotation = root * quat * TranDict[spine].offset;
				}
			}

			var shoulderDelta	= m_ShoulderInfo.GetSinDelta(f);
			if (m_ShoulderL)	Breath(m_ShoulderL, Quaternion.Euler(m_ShoulderLOffset) * Quaternion.Euler(m_ShoulderLRatio * shoulderDelta));
			if (m_ShoulderR)	Breath(m_ShoulderR, Quaternion.Euler(m_ShoulderROffset) * Quaternion.Euler(m_ShoulderRRatio * shoulderDelta));
			
			var upperArmDelta	= m_UpperArmInfo.GetSinDelta(f);
			if (m_UpperArmL)	Breath(m_UpperArmL, Quaternion.Euler(m_UpperArmLOffset) * Quaternion.Euler(m_UpperArmLRatio * upperArmDelta));
			if (m_UpperArmR)	Breath(m_UpperArmR, Quaternion.Euler(m_UpperArmROffset) * Quaternion.Euler(m_UpperArmRRatio * upperArmDelta));


			if (breastLArr != null && breastLArr.Count > 0)
			{
				var arr = breastLArr;
				var divisor = arr.Count < 2 ? 1f : 1f / (arr.Count - 1);
				for (int i = 0; i < arr.Count; ++i)
				{
					var offset = m_BreastCurve.Evaluate(i * divisor);
					var delta = Mathf.Sin(f + (m_BreastOffset + offset)) * m_BreastAmplitude;
					var euler = m_BreastLRatio * delta;
					Breath(arr[i].transform, Quaternion.Euler(euler));
				}
			}
			if (breastRArr != null && breastRArr.Count > 0)
			{
				var arr = breastRArr;
				var divisor = arr.Count < 2 ? 1f : 1f / (arr.Count - 1);
				for (int i = 0; i < arr.Count; ++i)
				{
					var offset = m_BreastCurve.Evaluate(i * divisor);
					var delta = Mathf.Sin(f + (m_BreastOffset + offset)) * m_BreastAmplitude;
					var euler = m_BreastRRatio * delta;
					Breath(arr[i].transform, Quaternion.Euler(euler));
				}
			}

			void Breath(Transform bone, Quaternion deltaQuat)
			{
				if (!TranDict.TryGetValue(bone, out var cache))
					return;
				bone.rotation = root * deltaQuat * cache.offset;
			}
		}


		private void OnDrawGizmos()
		{
			/*
			var root = animator.transform.rotation;
			foreach (var spine in m_Spines)
			{
				// GizmosExtend.DrawTransform(spine, false, 1f);
				var fixWorldQuat = spine.rotation * spineDict[spine].offset.Inverse();
				GizmosExtend.DrawTransform(spine.position, fixWorldQuat, false, 1f);
			}
			*/


		}
	}
}