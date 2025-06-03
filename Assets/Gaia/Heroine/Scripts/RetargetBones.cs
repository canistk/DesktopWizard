using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class RetargetBones : MonoBehaviour
	{
		[SerializeField] Animator m_Self = null;
		[SerializeField] BodyLayout m_Provider = null;

		[SerializeField] bool m_CopyScale = false;
		[SerializeField] List<string> m_ExtraBones = new List<string>();

		public void AddExtraBone(string name)
		{
			if (m_ExtraBones.Contains(name))
				return;
			m_ExtraBones.Add(name);
		}

		private void SetProvider(BodyLayout provider)
		{
			m_Provider = provider;
		}

		private void Reset()
		{
			m_Self = GetComponent<Animator>();
		}

		HumanBodyBones[] m_BoneKeys;

		Dictionary<HumanBodyBones, Transform> m_Bones;
		private void Awake()
		{
			m_Self.applyRootMotion = false;
			m_BoneKeys = (HumanBodyBones[])System.Enum.GetValues(typeof(HumanBodyBones));
			m_Bones = new Dictionary<HumanBodyBones, Transform>((int)HumanBodyBones.LastBone);
			foreach (var key in m_BoneKeys)
			{
				if (key == HumanBodyBones.LastBone)
					continue;
				var tmp = m_Self.GetBoneTransform(key);
				if (tmp == null)
					continue;
				m_Bones[key] = tmp;
			}
		}

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		private void LateUpdate()
		{
			if (m_Provider == null)
				return;

			var t = m_Provider.transform;
			m_Self.transform.SetPositionAndRotation(t.position, t.rotation);

			foreach (var key in m_BoneKeys)
			{
				if (key == HumanBodyBones.LastBone)
					continue;
				if (!m_Bones.ContainsKey(key))
					continue;
				var p = m_Provider.animator.GetBoneTransform(key);
				if (p == null)
					continue;

				if (m_CopyScale)
					m_Bones[key].localScale = p.localScale;
				m_Bones[key].SetPositionAndRotation(p.position, p.rotation);
			}

			for (int i = 0; i < m_ExtraBones.Count; ++i)
			{
				var bName = m_ExtraBones[i];
				if (string.IsNullOrEmpty(bName) || m_IgnoreTarget.Contains(bName))
					continue;
				if (!m_SelfBones.TryGetValue(bName, out var bone) &&
					!_TryCacheOrIgnoreBone(bName, out bone))
					continue;

				// we got it.
				if (!m_Provider.TryGetNamedTransform(bName, out var target))
					continue;
				bone.SetPositionAndRotation(target.position, target.rotation);
			}
		}

		Dictionary<string, Transform> m_SelfBones = new Dictionary<string, Transform>();
		HashSet<string> m_IgnoreTarget = new HashSet<string>();
		private bool _TryCacheOrIgnoreBone(string name, out Transform bone)
		{
			bone = _FindChildren(m_Self.transform, name);
			if (bone == null)
			{
				m_IgnoreTarget.Add(name);
				return false;
			}
			m_SelfBones[name] = bone;
			return bone != null;
		}
		protected Transform _FindChildren(Transform parent, string name)
		{
			var queue = new Queue<Transform>();
			queue.Enqueue(parent);
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (current.name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase))
					return current;
				if (current.childCount <= 0)
					continue;
				for (int i = 0; i < current.childCount; ++i)
					queue.Enqueue(current.GetChild(i));
			}
			return null;
		}
	}
}