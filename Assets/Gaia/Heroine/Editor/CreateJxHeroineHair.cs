using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Kit2;
using BoingKit;
namespace Gaia
{
	using Chain = BoingKit.BoingBones.Chain;

	public class CreateJxHeroineHair : CreatePrefabBase
	{
		public readonly GameObject fbx;
		public readonly string folder;
		public readonly string fileName;
		private GameObject m_Variant;
		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;

		public CreateJxHeroineHair(string path, GameObject fbx)
		{
			this.folder		= Path.GetDirectoryName(path);
			this.fileName	= Path.GetFileNameWithoutExtension(path);
			this.fbx		= fbx;
		}

		private int m_Step = 0;
		private System.Action[] m_StepArr;
		protected override void OnEnter()
		{
			Debug.Log($"{GetType().Name} started.");
			// MagicaClothSetup();
			BoingKitSetup();
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
			_WritePrefabVariant(folder, "Heroine_Hair", m_Variant, true);
		}

		private void BoingKitSetup()
		{
			BoingKit.BoingBones.Chain _BackChain(Transform obj)
			{
				return new BoingKit.BoingBones.Chain
				{
					Root = obj,
					EffectorReaction = true,
					LooseRoot = false,
					ParamsOverride = null,
					AnimationBlendCurveType = Chain.CurveType.RootOneTailZero,
					LengthStiffnessCurveType = Chain.CurveType.ConstantOne,
					PoseStiffnessCurveType = Chain.CurveType.ConstantHalf,
					MaxBendAngleCap = 180f,
					BendAngleCapCurveType = Chain.CurveType.ConstantOne,
					CollisionRadiusCurveType = Chain.CurveType.ConstantOne,
					MaxCollisionRadius = 0.055f,
					EnableBoingKitCollision = false,
					EnableUnityCollision = true,
					EnableInterChainCollision = true,
					Gravity = new Vector3(0, -9.81f, 0),
					SquashAndStretchCurveType = Chain.CurveType.ConstantZero,
				};
			}
			BoingKit.BoingBones.Chain _FrontChain(Transform obj)
			{
				return new BoingKit.BoingBones.Chain
				{
					Root = obj,
					EffectorReaction = true,
					LooseRoot = false,
					ParamsOverride = null,
					AnimationBlendCurveType = Chain.CurveType.RootOneTailZero,
					LengthStiffnessCurveType = Chain.CurveType.ConstantOne,
					PoseStiffnessCurveType = Chain.CurveType.RootOneTailZero,
					MaxBendAngleCap = 20f,
					BendAngleCapCurveType = Chain.CurveType.ConstantOne,
					CollisionRadiusCurveType = Chain.CurveType.RootOneTailHalf,
					MaxCollisionRadius = 0.01f,
					EnableBoingKitCollision = false,
					EnableUnityCollision = true,
					EnableInterChainCollision = true,
					Gravity = new Vector3(0, -9.81f, 0),
					SquashAndStretchCurveType = Chain.CurveType.ConstantZero,
				};
			}
			BoingKit.BoingBones.Chain _SideChain(Transform obj)
			{
				return new BoingKit.BoingBones.Chain
				{
					Root = obj,
					EffectorReaction = true,
					LooseRoot = false,
					ParamsOverride = null,
					AnimationBlendCurveType = Chain.CurveType.RootOneTailZero,
					LengthStiffnessCurveType = Chain.CurveType.ConstantOne,
					PoseStiffnessCurveType = Chain.CurveType.RootOneTailZero,
					MaxBendAngleCap = 30f,
					BendAngleCapCurveType = Chain.CurveType.ConstantOne,
					CollisionRadiusCurveType = Chain.CurveType.RootOneTailHalf,
					MaxCollisionRadius = 0.01f,
					EnableBoingKitCollision = false,
					EnableUnityCollision = true,
					EnableInterChainCollision = true,
					Gravity = new Vector3(0, -9.81f, 0),
					SquashAndStretchCurveType = Chain.CurveType.ConstantZero,
				};
			}

			m_StepArr = new System.Action[]
			{
				() => {
					m_Variant = (GameObject)PrefabUtility.InstantiatePrefab(fbx);

				},
				() => {
					var root = m_Variant.transform;
					var hairRoots = new List<BoingKit.BoingBones.Chain>();
					var cnt = root.childCount;
					for (int i = 0; i < cnt; ++i)
					{
						var child = root.GetChild(i);
						if (!child.name.Contains("hair", IGNORE))
							continue;
						var arr = child.name.Split('_');
						if (arr.Length != 2)
						{
							Debug.LogError($"Invalid formation, assume `Hair_XX`, name={child.name}");
							continue;
						}
						var suffix = arr[1].ToLower();
						switch(suffix)
						{
							case "b1":
							{
								var
								obj = _FindChildren(root, "DEF-Hair.tentacle.B1.Main.001");		if (obj) hairRoots.Add(_BackChain(obj));
							}
							break;
							case "b2":
							{
								var
								obj = _FindChildren(root, "DEF-Hair.tentacle.R2.A1.001.L");		if (obj) hairRoots.Add(_SideChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.R2.A1.001.R");		if (obj) hairRoots.Add(_SideChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.RA.A1.001.L");     if (obj) hairRoots.Add(_SideChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.RA.A1.001.R");     if (obj) hairRoots.Add(_SideChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.T.001");           if (obj) hairRoots.Add(_SideChain(obj));
							}
							break;
							case "b3":
							{
								var
								obj = _FindChildren(root, "DEF-Hair.tentacle.B3.Main.001");      if (obj) hairRoots.Add(_BackChain(obj));
							}
							break;
							case "f":
							{
								var
								obj = _FindChildren(root, "DEF-Hair.tentacle.F.A1.001");      if (obj) hairRoots.Add(_FrontChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.F.A2.001");      if (obj) hairRoots.Add(_FrontChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.F.A3.001");      if (obj) hairRoots.Add(_FrontChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.F.B1.001");      if (obj) hairRoots.Add(_FrontChain(obj));
								obj = _FindChildren(root, "DEF-Hair.tentacle.F.B2.001");      if (obj) hairRoots.Add(_FrontChain(obj));
							}
							break;
						}
					}

					var bb = root.GetOrAddComponent<BoingKit.BoingBones>();
					bb.UpdateMode = BoingManager.UpdateMode.FixedUpdate;
					bb.BoneChains = hairRoots.ToArray();
				},
			};
		}
	}
}