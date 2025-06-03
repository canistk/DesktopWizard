using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
namespace Gaia
{
    public class GxCtrlSpring : MonoBehaviour
    {
		// TODO: hide after setup completed in inspector
		public Transform[] m_BoneRef;

		public bool m_InverseCurve = false;
		[RectRange] public AnimationCurve m_RadiusCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Min(0f)] public float m_Radius = 1f;

		[RectRange] public AnimationCurve m_DampCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Min(0f)] public float m_Dampping = 0.2f;

		[RectRange] public AnimationCurve m_SpringCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Min(0f)] public float m_Spring = 10f;

		[RectRange] public AnimationCurve m_MassCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Min(0f)] public float m_Mass = 1f;

		[RectRange] public AnimationCurve m_DragCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Min(0f)] public float m_Drag = 0.1f;

		[RectRange] public AnimationCurve m_AngularDragCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Min(0f)] public float m_AngularDrag = 0.1f;

		[RectRange] public AnimationCurve m_StretchCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Min(0f)] public float m_Stretch = 0;

		[Space]
		[SerializeField]
		public Bone[] m_Bones;
		[System.Serializable]
		public class Bone
		{
			public Transform transform;
			public Rigidbody rigidbody;
			public SpringJoint joint;
			public SphereCollider collider;

			public Bone(Transform transform)
			{
				this.transform = transform;
				this.rigidbody = transform.GetOrAddComponent<Rigidbody>();
				this.joint = transform.GetOrAddComponent<SpringJoint>();
				this.collider = transform.GetOrAddComponent<SphereCollider>();
			}
		}

		private void OnValidate()
		{
			if (m_Bones == null)
				return;
			for (int i = 0; i < m_Bones.Length; ++i)
			{
				var pt = (float)i / m_Bones.Length;
				if (m_InverseCurve)
					pt = 1f - pt;
				var b = m_Bones[i];

				b.collider.radius	= Mathf.Max(0f, m_RadiusCurve.Evaluate(pt) * m_Radius);

				b.joint.spring		= Mathf.Max(0f, m_SpringCurve.Evaluate(pt) * m_Spring);
				b.joint.damper		= Mathf.Max(0f, m_DampCurve.Evaluate(pt) * m_Dampping);
				b.joint.maxDistance	= Mathf.Max(float.Epsilon, m_StretchCurve.Evaluate(pt) * m_Stretch);
				if (b.joint.minDistance > b.joint.maxDistance)
					b.joint.minDistance = b.joint.maxDistance;

				b.rigidbody.mass	= Mathf.Max(0f, m_MassCurve.Evaluate(pt) * m_Mass);
				b.rigidbody.drag	= Mathf.Max(0f, m_DragCurve.Evaluate(pt) * m_Drag);
				b.rigidbody.angularDrag = Mathf.Max(0f, m_AngularDragCurve.Evaluate(pt) * m_AngularDrag);
			}
		}

		[ContextMenu("Init by bones")]
		public void Init()
		{
			if (m_BoneRef == null || m_BoneRef.Length == 0)
			{
				Debug.LogError("Fail to init by bones.");
				return;
			}
			m_Bones = new Bone[m_BoneRef.Length];
			
			for (int i = 0; i < m_BoneRef.Length; ++i)
			{
				var b = m_Bones[i] = new Bone(m_BoneRef[i]);
				if (i == 0)
				{
					b.rigidbody.isKinematic = true;
				}
				else 
				{
					var lastBone = m_Bones[i - 1];
					b.joint.connectedBody = lastBone.rigidbody;
					// b.joint.connectedArticulationBody ;
					b.joint.autoConfigureConnectedAnchor = false;
					b.joint.connectedAnchor = lastBone.transform.InverseTransformPoint(b.transform.position);
				}
			}
		}
	}
}