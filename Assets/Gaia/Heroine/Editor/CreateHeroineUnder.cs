using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Kit2;
using UnityEditor;
namespace Gaia
{
    public class CreateHeroineUnder : CreatePrefabBase
	{
		public readonly GameObject fbx;
		public readonly string folder;
		public readonly string fileName;
		private GameObject m_Variant;
		public CreateHeroineUnder(string path, GameObject fbx)
		{
			this.folder = Path.GetDirectoryName(path);
			this.fileName = Path.GetFileNameWithoutExtension(path);
			this.fbx = fbx;
		}
		private Animator m_Animator;
		private int m_Step = 0;
		private System.Action[] m_StepArr;
		protected override void OnEnter()
		{
			Debug.Log($"{GetType().Name} started.");
			m_StepArr = new System.Action[]
			{
				() =>
				{
					m_Variant = GameObject.Instantiate(fbx); // not prefab variant
					m_Animator = m_Variant.GetOrAddComponent<Animator>();
					m_Animator.applyRootMotion = false;
				},
				() => // disable renderers then Under
				{
					var rs = m_Variant.GetComponentsInChildren<Renderer>();
					for (int i = 0; i < rs.Length; ++i)
					{
						if (rs[i] is SkinnedMeshRenderer smr)
						{
							smr.updateWhenOffscreen = true;
						}
						rs[i].receiveShadows = false;
						if (rs[i].name.Contains("under", System.StringComparison.InvariantCultureIgnoreCase))
							continue;
						GameObject.DestroyImmediate(rs[i].gameObject);
					}
				},
				() =>
				{
					var retarget = m_Variant.GetOrAddComponent<RetargetBones>();
					retarget.AddExtraBone("DEF-Hip.Follow.L");
					retarget.AddExtraBone("DEF-Hip.Follow.R");
					retarget.AddExtraBone("DEF-Thigh.Follow.L");
					retarget.AddExtraBone("DEF-Thigh.Follow.R");
				},
				() => {
					//_ParentConstraint(m_Variant.transform, "DEF-Hip.Follow.L", "DEF-thigh.L", 0.728f);
					//_ParentConstraint(m_Variant.transform, "DEF-Hip.Follow.R", "DEF-thigh.R", 0.728f);
					//_ParentConstraint(m_Variant.transform, "DEF-Thigh.Follow.L", "DEF-thigh.L", 0.9f);
					//_ParentConstraint(m_Variant.transform, "DEF-Thigh.Follow.R", "DEF-thigh.R", 0.9f);
					
				},
				() => // remove non-used bones
				{
					_TryRemoveChildrens(m_Variant.transform, (t) =>
					{
						return
						t.name.Contains("head", System.StringComparison.InvariantCultureIgnoreCase) ||
						t.name.Contains("body", System.StringComparison.InvariantCultureIgnoreCase) ||
						t.name.Contains("TShirt", System.StringComparison.InvariantCultureIgnoreCase) ||
						t.name.Contains("Hair", System.StringComparison.InvariantCultureIgnoreCase);
					});
				}
			};
		}

		protected override bool ContinueOnNextCycle()
		{
			if (m_Step >= m_StepArr.Length)
				return false;
			m_StepArr[m_Step]?.Invoke();
			m_Step++;
			return true;
		}

		protected override void OnComplete()
		{
			_WritePrefabVariant(folder, fileName + "_Under", m_Variant, true);
		}
	}
}