using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
namespace Gaia
{
	[System.Serializable]
	public class GxCtrlBone
	{
		public Transform transform;
		public List<OffsetData> data;
		public void Init(Transform transform)
		{
			this.transform = transform;
			this.data = new List<OffsetData>();
		}
		public void CleanData()
		{
			data.Clear();
		}
		public void AddData(Transform target, float weight)
		{
			this.data.Add(new OffsetData(target, transform, weight));
		}

		/// <summary>
		/// Cache position & rotation offset related from <see cref="GxCtrlBone"/>
		/// to <see cref="target"/> bone.
		/// </summary>
		[System.Serializable]
		public class OffsetData
		{
			public Transform target;
			public Vector3 displacement;
			public Vector3 angular;
			[Min(0f)] public float weight = 1f;

			public OffsetData() {}
			public OffsetData(Transform target, Transform anchor, float weight) :
				this(target,
					target.InverseTransformPoint(anchor.position),
					(target.rotation.Inverse() * anchor.rotation).eulerAngles,
					weight)
			{}
			public OffsetData(Transform target, Vector3 displacement, Vector3 angular, float weight)
			{
				this.target = target;
				this.displacement = displacement;
				this.angular = angular;
				this.weight = weight;
			}

			internal void CacheOffset(GxCtrlBone ctrl)
			{
				var src = ctrl.transform;
				displacement = target.InverseTransformPoint(src.position);
				angular = (target.rotation.Inverse() * src.rotation).eulerAngles;
			}
		}

		public void CacheOffset()
		{
			foreach (var c in data)
			{
				c.CacheOffset(this);
			}
		}

		/// <summary>
		/// Prepare the position for <see cref="CalcCoordinate(out Vector3, out Quaternion)"/>
		/// </summary>
		/// <param name="cnt">amount that invoke </param>
		/// <param name="ps">position array</param>
		/// <param name="rs">rotation array</param>
		/// <param name="ws">weight array</param>
		public void PrepareData(out int cnt, out Vector3[] ps, out Quaternion[] rs, out float[] ws)
		{
			cnt = data.Count;
			ps = new Vector3[cnt];
			rs = new Quaternion[cnt];
			ws = new float[cnt];
			for (var i = 0; i < cnt; ++i)
			{
				var c = data[i];
				ps[i] = c.target.TransformPoint(c.displacement);
				rs[i] = c.target.rotation * Quaternion.Euler(c.angular);
				ws[i] = Mathf.Max(0f, c.weight); // no negative value
			}
		}

		/// <summary>
		/// Calculate the coordinate at current frame.
		/// <see cref="ApplyCoordinate(float)"/>
		/// </summary>
		/// <param name="pos">current frame position</param>
		/// <param name="rot">current frame rotation</param>
		public void CalcCoordinate(out Vector3 pos, out Quaternion rot)
		{
			PrepareData(out _, out Vector3[] ps, out Quaternion[] rs, out float[] ws);
			// Step 2, Find target coordinate this frame.
			pos = Vector3Extend.Centroid(ps, ws);
			rot = QuaternionExtend.WeightedAverage(rs, ws);
		}

		public void ApplyCoordinate(float speed = 1f)
		{
			if (transform == null)
				return;
			CalcCoordinate(out var tp, out var tr);
			// Step 3, lerp between the current & target coordinate
			var t = Mathf.Max(float.Epsilon, Time.deltaTime * speed);
			var p = Vector3.Lerp(transform.position, tp, t);
			var r = Quaternion.Slerp(transform.rotation, tr, t);
			transform.SetPositionAndRotation(p, r);
		}
	}

}