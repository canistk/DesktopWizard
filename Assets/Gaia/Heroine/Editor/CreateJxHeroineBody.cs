using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Kit2;
using BoingKit;
namespace Gaia
{
    public class CreateJxHeroineBody : CreatePrefabBase
	{
		public readonly GameObject fbx;
		public readonly string folder;
		public readonly string fileName;
		private GameObject m_Variant, m_HairVariant;
		private Animator m_Animator;
		private BodyLayout m_BodyLayout;
		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;

		public CreateJxHeroineBody(string path, GameObject fbx)
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
				() => // find head
				{
					m_Head = _FindChildren(m_Variant.transform, "Head");
					m_FaceRig = m_Head.transform.GetOrAddComponent<FaceRig>();
					if (m_FaceRig == null)
					{
						Debug.LogError("FaceRig not found.");
						return;
					}
					// m_BodyLayout.faceRig = m_FaceRig;
					m_FaceRig.m_BS_MaxWeight = 100f;

					// m_FaceRig.m_Database
					string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
					var found = false;
					foreach (string guid in guids)
					{
						string path = AssetDatabase.GUIDToAssetPath(guid);
						var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
						if (obj is FaceRigDatabase faceRigDb)
						{
							m_FaceRig.m_Database = faceRigDb;
							found = true;
							break;
						}
					}
					if (!found)
					{
						Debug.LogError("FaceRigDatabase not found.");
						return;
					}

					m_FaceRig.Editor_FetchDatabase();
				},
				() =>
				{
					// Merge hair prefab variant based on head transform
					var hairPath = Path.Combine(folder, "Heroine_Hair.prefab");
					var hairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hairPath);
					if (hairPrefab == null)
					{
						Debug.LogError($"Fail to locate hair in {hairPath}.");
					}
					else
					{
						var headPivot = m_Animator.GetBoneTransform(HumanBodyBones.Head);
						m_HairVariant = (GameObject)PrefabUtility.InstantiatePrefab(hairPrefab);
						if (m_Head != null && headPivot != null)
						{
							m_HairVariant.transform.SetParent(headPivot, true);
						}
					}
				},
#if false
				() => {
					// setup collider bone

					MagicaColliderBoneSetup.ExecuteAutoSetup(m_Animator);
				},
				() =>
				{
					// MagicaCloth setup
					if (m_HairVariant != null)
					{
						var bones = new List<MagicaCloth2.ColliderComponent>(m_Variant.GetComponentsInChildren<MagicaCloth2.ColliderComponent>());
						var characterLayer = LayerMask.NameToLayer("Character");
						bones.RemoveAll(o => o.gameObject.layer != characterLayer);
						Debug.Log($"Total bones found : {bones.Count}");

						var root = m_HairVariant.transform;
						var mcs = m_HairVariant.transform.GetComponentsInChildren<MagicaCloth>();
						var cnt = mcs.Length;
						for (int i = 0; i < cnt; ++i)
						{
							var tentacle = mcs[i];
							var mc = tentacle.SerializeData;
							mc.colliderCollisionConstraint.colliderList.AddRange(bones);
						}
					}
				},
#endif

#if true
				() => {
					// setup collider bone
					U3DColliderBoneSetup.ExecuteAutoSetup(m_Animator);
				},
				() =>
				{
					// Boing Kit setup
					if (m_HairVariant != null)
					{
						var bones = new List<Collider>(m_Variant.GetComponentsInChildren<Collider>());
						var characterLayer = LayerMask.NameToLayer("Character");
						bones.RemoveAll(o => o.gameObject.layer != characterLayer);

						var root = m_HairVariant.transform;
						var bb = m_HairVariant.transform.GetComponentInChildren<BoingKit.BoingBones>();
						var cnt = bb.BoneChains.Length;
						bb.UnityColliders = bones.ToArray();
					}
				},
#endif
				() =>
				{
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
					m_BodyLayout.Editor_Fetch();
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
			_WritePrefabVariant(folder, "Heroine", m_Variant, true);
		}
	}
}