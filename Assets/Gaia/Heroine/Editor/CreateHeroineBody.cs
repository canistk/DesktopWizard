using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Kit2;
namespace Gaia
{
    public class CreateHeroineBody : CreatePrefabBase
	{
		public readonly GameObject fbx;
		public readonly string folder;
		public readonly string fileName;
		private GameObject m_Variant;
		private Animator m_Animator;
		private BodyLayout m_BodyLayout;
		public CreateHeroineBody(string path, GameObject fbx)
		{
			this.folder		= Path.GetDirectoryName(path);
			this.fileName	= Path.GetFileNameWithoutExtension(path);
			this.fbx		= fbx;
		}
		private FaceRig m_FaceRig;
		private Transform m_Head;
		private int m_Step = 0;
		private System.Action[] m_StepArr;
		protected override void OnEnter()
		{
			Debug.Log($"{GetType().Name} started.");
			m_StepArr = new System.Action[]
			{
				() => {
					m_Variant = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
					m_Animator = m_Variant.GetOrAddComponent<Animator>();
					m_Animator.applyRootMotion = false;
					m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
					m_BodyLayout = m_Variant.GetOrAddComponent<BodyLayout>();
				},
				() => {
					// setup collider bone
					var breastL = _FindChildren(m_Variant.transform, "DEF-breast.L");
					if (breastL)
					{
						m_BodyLayout.AddNamedTransfrom(breastL.transform);
						var lsc = breastL.GetOrAddComponent<SphereCollider>();
						lsc.radius = 0.0005f;
					}
					var breastR = _FindChildren(m_Variant.transform, "DEF-breast.R");
					if (breastR)
					{
						m_BodyLayout.AddNamedTransfrom(breastR.transform);
						var rsc = breastR.GetOrAddComponent<SphereCollider>();
						rsc.radius = 0.0005f;
					}
				},
				() => {
					_ParentConstraint(m_Variant.transform, "DEF-Hip.Follow.L", "DEF-thigh.L", 0.728f);
					_ParentConstraint(m_Variant.transform, "DEF-Hip.Follow.R", "DEF-thigh.R", 0.728f);
					_ParentConstraint(m_Variant.transform, "DEF-Thigh.Follow.L", "DEF-thigh.L", 0.9f);
					_ParentConstraint(m_Variant.transform, "DEF-Thigh.Follow.R", "DEF-thigh.R", 0.9f);
					
					if (_FindChildren(m_Variant.transform, "DEF-Hip.Follow.L") is Transform hfl)
						m_BodyLayout.AddNamedTransfrom(hfl);
					if (_FindChildren(m_Variant.transform, "DEF-Hip.Follow.R") is Transform hfr)
						m_BodyLayout.AddNamedTransfrom(hfr);
					if (_FindChildren(m_Variant.transform, "DEF-Thigh.Follow.L") is Transform tfl)
						m_BodyLayout.AddNamedTransfrom(tfl);
					if (_FindChildren(m_Variant.transform, "DEF-Thigh.Follow.R") is Transform tfr)
						m_BodyLayout.AddNamedTransfrom(tfr);
				},
				() => {
					// remove renderers other then head & body
					{
						var rs = m_Variant.GetComponentsInChildren<Renderer>();
						for (int i = 0; i < rs.Length; ++i)
						{
							if (rs[i] is SkinnedMeshRenderer smr)
							{
								smr.updateWhenOffscreen = true;
							}
							rs[i].receiveShadows = false;
							if (rs[i].name.Equals("Body", System.StringComparison.InvariantCultureIgnoreCase) ||
								rs[i].name.Equals("Head", System.StringComparison.InvariantCultureIgnoreCase))
								continue;
							//rs[i].gameObject.SetActive(false);
							GameObject.DestroyImmediate(rs[i].gameObject);
						}
					}
				},
				() => // find head
				{
					m_Head = _FindChildren(m_Variant.transform, "Head");
					m_FaceRig = m_Head.transform.GetOrAddComponent<FaceRig>();
					m_FaceRig.m_BS_MaxWeight = 100f;
					var emotion = m_Variant.GetOrAddComponent<EmotionWheel>();
					emotion.m_FaceRig = m_FaceRig;
					var teethFix = m_Head.GetOrAddComponent<TeethHotfix>();
					teethFix.m_FaceRig = m_FaceRig;
				},
				() => // eye ctrl
				{
					var eyeCtrl = m_Variant.GetOrAddComponent<HeroineEyeCtrl>();
					eyeCtrl.m_FaceRig = m_FaceRig;
					eyeCtrl.m_Head = m_Animator.GetBoneTransform(HumanBodyBones.Head);
				},
				() =>
				{
					_TryRemoveChildrens(m_Variant.transform, (t) =>
					{
						return
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
			_WritePrefabVariant(folder, fileName + "_Body", m_Variant, true);
		}
	}
}