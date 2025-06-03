using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Kit2;
namespace Gaia
{
    public class CreateHeroineTShirt : CreatePrefabBase
	{
		public readonly GameObject fbx;
		public readonly string folder;
		public readonly string fileName;
		private GameObject m_Variant;
		private Animator m_Animator;
		private int m_Step = 0;
		private System.Action[] m_StepArr;
		public CreateHeroineTShirt(string path, GameObject fbx)
		{
			this.folder = Path.GetDirectoryName(path);
			this.fileName = Path.GetFileNameWithoutExtension(path);
			this.fbx = fbx;
		}
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
						if (rs[i].name.Contains("TShirt", System.StringComparison.InvariantCultureIgnoreCase))
							continue;
						GameObject.DestroyImmediate(rs[i].gameObject);
					}
				},
				() =>
				{
					var retarget = m_Variant.GetOrAddComponent<RetargetBones>();
					retarget.AddExtraBone("DEF-breast.L");
					retarget.AddExtraBone("DEF-breast.R");
					retarget.AddExtraBone("DEF-Hip.Follow.L");
					retarget.AddExtraBone("DEF-Hip.Follow.R");
					retarget.AddExtraBone("DEF-Thigh.Follow.L");
					retarget.AddExtraBone("DEF-Thigh.Follow.R");
				},
				() =>
				{
					if (!m_Animator.isHuman)
						return;

					var comp = m_Variant.GetOrAddComponent<TShirtCtrlV2>();
					var chest = m_Animator.GetBoneTransform(HumanBodyBones.UpperChest);
					var LUA = m_Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
					var LLA = m_Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
					var RUA = m_Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
					var RLA = m_Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

					_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.Hand.Constraint.B.L"), out var bl);
					comp.shirtBL = new GxCtrlBone();
					comp.shirtBL.Init(bl);
					comp.shirtBL.AddData(chest, 1f);
					comp.shirtBL.AddData(LUA, 1f);
					_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.Hand.Constraint.B.R"), out var br);
					comp.shirtBR = new GxCtrlBone();
					comp.shirtBR.Init(br);
					comp.shirtBR.AddData(chest, 1f);
					comp.shirtBR.AddData(RUA, 1f);

					_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.Hand.Constraint.F.L"), out var fl);
					comp.shirtFL = new GxCtrlBone();
					comp.shirtFL.Init(fl);
					comp.shirtFL.AddData(chest, 1f);
					comp.shirtFL.AddData(LUA, 1f);
					_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.Hand.Constraint.F.R"), out var fr);
					comp.shirtFR = new GxCtrlBone();
					comp.shirtFR.Init(fr);
					comp.shirtFR.AddData(chest, 1f);
					comp.shirtFR.AddData(RUA, 1f);

					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.B.L"), out r.LB);
					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.D.L"), out r.LD);
					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.F.L"), out r.LF);
					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.T.L"), out r.LT);

					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.B.R"), out r.RB);
					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.D.R"), out r.RD);
					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.F.R"), out r.RF);
					//_TryFindChild(m_Variant.transform, (o) => o.name.Equals("DEF-TShirt.tentacle.Hand.Follow.001.T.R"), out r.RT);

				},
				() => // remove non-used bones
				{
					_TryRemoveChildrens(m_Variant.transform, (t) =>
					{
						return
						t.name.Contains("head", System.StringComparison.InvariantCultureIgnoreCase) ||
						t.name.Contains("body", System.StringComparison.InvariantCultureIgnoreCase) ||
						t.name.Contains("under", System.StringComparison.InvariantCultureIgnoreCase) ||
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
			_WritePrefabVariant(folder, fileName + "_TShirt", m_Variant, true);
		}
	}
}