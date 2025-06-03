using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Kit2;
namespace Gaia
{
    public class CreateHeroineHair : CreatePrefabBase
	{
		public readonly GameObject fbx;
		public readonly string folder;
		public readonly string fileName;
		private GameObject m_Variant;
		private Animator m_Animator;
		private int m_Step = 0;
		private System.Action[] m_StepArr;
		public CreateHeroineHair(string path, GameObject fbx)
		{
			this.folder		= Path.GetDirectoryName(path);
			this.fileName	= Path.GetFileNameWithoutExtension(path);
			this.fbx		= fbx;
		}
		protected override void OnEnter()
		{
			Debug.Log($"{GetType().Name} started.");
			m_StepArr = new System.Action[]
			{
				() =>
				{
					//m_Variant = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
					m_Variant = GameObject.Instantiate(fbx); // not prefab variant
					m_Animator = m_Variant.GetOrAddComponent<Animator>();
					m_Animator.applyRootMotion = false;
				},
				() => // disable renderers then hair
				{
					var rs = m_Variant.GetComponentsInChildren<Renderer>();
					for (int i = 0; i < rs.Length; ++i)
					{
						if (rs[i] is SkinnedMeshRenderer smr)
						{
							smr.updateWhenOffscreen = true;
						}
						rs[i].receiveShadows = false;
						if (rs[i].name.Contains("hair", System.StringComparison.InvariantCultureIgnoreCase))
							continue;
						//rs[i].gameObject.SetActive(false);
						GameObject.DestroyImmediate(rs[i].gameObject);
					}
				},
				() =>
				{
					Hotfix_FiskHairReparent();
				},
				() =>
				{
					var retarget = m_Variant.GetOrAddComponent<RetargetBones>();
				},
				() =>
				{
					_TryRemoveChildrens(m_Variant.transform, (t) =>
					{
						return
						t.name.Contains("TShirt", System.StringComparison.InvariantCultureIgnoreCase);
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
			_WritePrefabVariant(folder, fileName + "_Hair", m_Variant, true);
		}


		private struct HairStruct
		{
			public string parent;
			public string[] child;

			public HairStruct(string parent, params string[] child)
			{
				this.parent = parent;
				this.child = child;
			}
		}

		private void Hotfix_FiskHairReparent()
		{
			// Due to Blender hair bone naming issue, we need to reparent the hair bones
			var head = m_Animator.GetBoneTransform(HumanBodyBones.Head);
			if (head == null)
				return;

			var hairB1 = _FindOrCreateChild(head, "Hair.B1.Group");
			var hairB2 = _FindOrCreateChild(head, "Hair.B2.Group");
			var hairB3 = _FindOrCreateChild(head, "Hair.B3.Group");
			var hairF = _FindOrCreateChild(head, "Hair.F.Group");

			var data = new[]
			{
			// parent[0], rest are children(s)
			new [] { hairB1.name,
				"DEF-Hair.tentacle.B1.Main.001",
				},
			new [] { "DEF-Hair.tentacle.B1.Main.001",
				"DEF-Hair.tentacle.B1.Follow.001",
				},
			new [] { "DEF-Hair.tentacle.B1.Main.003",
				"DEF-Hair.tentacle.B1.A.001.L",
				"DEF-Hair.tentacle.B1.A.001.R",
				"DEF-Hair.tentacle.B1.B.001.L",
				"DEF-Hair.tentacle.B1.B.001.R",
				"DEF-Hair.tentacle.B1.C.001",
				},
			new [] { hairB3.name,
				"DEF-Hair.tentacle.B3.Main.001",
				},
			new [] { "DEF-Hair.tentacle.B3.Main.002",
				"DEF-Hair.tentacle.B3.A.001.L",
				"DEF-Hair.tentacle.B3.A.001.R",
				"DEF-Hair.tentacle.B3.B.001.L",
				"DEF-Hair.tentacle.B3.B.001.R",
				"DEF-Hair.tentacle.B3.C.001",
				},
			new [] { hairB2.name,
				"DEF-Hair.tentacle.R2.A1.001.L",
				"DEF-Hair.tentacle.R2.A1.001.R",
				"DEF-Hair.tentacle.RA.A1.001.L",
				"DEF-Hair.tentacle.RA.A1.001.R",
				"DEF-Hair.tentacle.T.001",
				},
			new [] {
				"DEF-Hair.tentacle.RA.A1.002.L",
				"DEF-Hair.tentacle.RA.A2.001.L",
				},
			new [] {
				"DEF-Hair.tentacle.RA.A1.002.R",
				"DEF-Hair.tentacle.RA.A2.001.R",
				},
			new [] { hairF.name,
				"DEF-Hair.tentacle.F.A2.001",
				"DEF-Hair.tentacle.F.A3.001",
				"DEF-Hair.tentacle.F.A1.001",
				"DEF-Hair.tentacle.F.B1.001",
				"DEF-Hair.tentacle.F.B2.001",
				},
			};

			
			string[] cache = { };
			int i = 0;
			try
			{
				for (i = 0; i < data.Length; i++)
				{
					cache = data[i];
					_TryReparent(data[i], head);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError(ex.Message);
				Debug.LogError($"Fail to reparent hair bones. case[{i}]\n{cache[0]}");
			}
			return;

		}
	}
}