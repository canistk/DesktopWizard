using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
using UnityEditor;
using Codice.CM.Common.Tree.Partial;

namespace Gaia
{
	[CustomEditor(typeof(FaceRig))]
	public class FaceRigEditor : EditorBase
    {
		SerializedProperty m_DatabaseProp, m_EyeMethodProp;
		SerializedProperty
			m_BlinkConfigProp,
			m_BlinkLeftEyeProp, m_BlinkRightEyeProp,

			m_BS_LeftEyeProp, m_BS_RightEyeProp,
			m_BS_XAxisBiasProp, m_BS_YAxisBiasProp,

			m_TargetProp, m_DistractProp,
			m_lEyeProp, m_rEyeProp, m_lRotFixProp, m_rRotFixProp, m_hRotFixProp,
			m_EyeSignClampAngleProp,

			m_BS_MaxWeightProp;
		protected override void OnEnable()
		{
			base.OnEnable();

			m_DatabaseProp		= serializedObject.FindProperty(nameof(FaceRig.m_Database));
			m_EyeMethodProp		= serializedObject.FindProperty(nameof(FaceRig.m_EyeMethod));
			m_BS_MaxWeightProp	= serializedObject.FindProperty(nameof(FaceRig.m_BS_MaxWeight));

			// Blink
			m_BlinkLeftEyeProp	= serializedObject.FindProperty(nameof(FaceRig.m_BlinkLeftEye));
			m_BlinkRightEyeProp	= serializedObject.FindProperty(nameof(FaceRig.m_BlinkRightEye));
			m_BlinkConfigProp	= serializedObject.FindProperty(nameof(FaceRig.m_BlinkConfig));

			// Look At (BlendShape)
			m_BS_LeftEyeProp	= serializedObject.FindProperty(nameof(FaceRig.m_BS_LeftEye));
			m_BS_RightEyeProp	= serializedObject.FindProperty(nameof(FaceRig.m_BS_RightEye));
			m_BS_XAxisBiasProp	= serializedObject.FindProperty(nameof(FaceRig.m_BS_XAxisBias));
			m_BS_YAxisBiasProp	= serializedObject.FindProperty(nameof(FaceRig.m_BS_YAxisBias));

			// Look At (Transform)
			m_lEyeProp			= serializedObject.FindProperty(nameof(FaceRig.m_lEye));
			m_rEyeProp			= serializedObject.FindProperty(nameof(FaceRig.m_rEye));
			m_hRotFixProp		= serializedObject.FindProperty(nameof(FaceRig.m_hRotFix));
			m_lRotFixProp		= serializedObject.FindProperty(nameof(FaceRig.m_lRotFix));
			m_rRotFixProp		= serializedObject.FindProperty(nameof(FaceRig.m_rRotFix));
			m_EyeSignClampAngleProp = serializedObject.FindProperty(nameof(FaceRig.m_EyeSignClampAngle));

			m_TargetProp		= serializedObject.FindProperty(nameof(FaceRig.m_Target));
			m_DistractProp		= serializedObject.FindProperty(nameof(FaceRig.m_DistractConfig));
		}

		protected override void OnBeforeDrawGUI()
		{
			base.OnBeforeDrawGUI();
		}

		protected override void OnDrawProperty(SerializedProperty property)
		{
			var isBlendShape = _IsBlendShape();

			if (_Is(property, m_DatabaseProp))
			{
				if (property.objectReferenceValue == null)
				{
					EditorGUILayout.HelpBox("Please assign FaceRigDatabase", MessageType.Warning);
					if (GUILayout.Button("Create FaceRigDatabase"))
					{
						var faceRig = target as FaceRig;
						faceRig.Editor_FetchDatabase();
					}
					EditorGUILayout.PropertyField(property, includeChildren: true);
				}
				else
				{
					_DrawOrginal(property);
				}
				EditorGUILayout.Separator();
			}
			else if (_Is(property, m_BlinkLeftEyeProp, m_BlinkRightEyeProp, m_BlinkConfigProp))
			{
				/// <see cref="DrawBlinkProps"/>
				return;
			}
			else if (_Is(property, m_BS_LeftEyeProp, m_BS_RightEyeProp, m_BS_XAxisBiasProp, m_BS_YAxisBiasProp, m_BS_MaxWeightProp))
			{
				/// <see cref="DrawLookAt_BlendShape"/>
				return;
			}
			else if (_Is(property, m_rEyeProp, m_lEyeProp, m_lEyeProp, m_rEyeProp,
				m_lRotFixProp, m_rRotFixProp, m_hRotFixProp, m_EyeSignClampAngleProp))
			{
				/// <see cref="DrawLookAt_Bone"/>
				return;
			}
			else if (_Is(property, m_TargetProp, m_DistractProp))
			{
				/// <see cref="DrawTargetConfig"/>
				return;
			}
			else
			{
				_DrawOrginal(property);
			}


			void _DrawOrginal(SerializedProperty prop)
			{
				base.OnDrawProperty(prop);
			}
		}

		protected override void OnAfterDrawGUI()
		{
			base.OnAfterDrawGUI();
			
			if (_IsBlendShape())
			{
				DrawLookAt_BlendShape();
			}
			else
			{
				DrawLookAt_Bone();
			}

			EditorGUILayout.Separator();
			DrawBlinkProps();
		}

		void DrawTargetConfig()
		{
			EditorGUILayout.PropertyField(m_TargetProp, includeChildren: true);
			EditorGUILayout.PropertyField(m_DistractProp, includeChildren: true);
		}

		void DrawLookAt_Bone()
		{
			if (_IsBlendShape())
				throw new System.Exception();
			EditorGUILayout.HelpBox("Eye Look At (humanoid)", MessageType.Info);
			DrawTargetConfig();

			EditorGUILayout.PropertyField(m_lEyeProp);
			EditorGUILayout.PropertyField(m_rEyeProp);
			EditorGUILayout.PropertyField(m_hRotFixProp);
			EditorGUILayout.PropertyField(m_lRotFixProp);
			EditorGUILayout.PropertyField(m_rRotFixProp);

			EditorGUILayout.HelpBox("Clamp eye sign angle within", MessageType.Info);
			EditorGUILayout.PropertyField(m_EyeSignClampAngleProp);
		}

		void DrawLookAt_BlendShape()
		{
			if (!_IsBlendShape())
				throw new System.Exception();

			EditorGUILayout.HelpBox("Eye Look At", MessageType.Info);
			DrawTargetConfig();
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Left Eye BlendShape");
			_DrawEye(m_BS_LeftEyeProp);
			EditorGUILayout.LabelField("Right Eye BlendShape");
			_DrawEye(m_BS_RightEyeProp);
			EditorGUILayout.Separator();

			EditorGUILayout.HelpBox("Multiple for .", MessageType.Info);
			EditorGUILayout.PropertyField(m_BS_XAxisBiasProp);
			EditorGUILayout.PropertyField(m_BS_YAxisBiasProp);

			void _DrawEye(SerializedProperty property)
			{
				SerializedProperty obj;
				obj = property.FindPropertyRelative(nameof(BlendShapeLookAt.up));
				_DrawBlendShape(obj);

				obj = property.FindPropertyRelative(nameof(BlendShapeLookAt.down));
				_DrawBlendShape(obj);

				obj = property.FindPropertyRelative(nameof(BlendShapeLookAt.left));
				_DrawBlendShape(obj);

				obj = property.FindPropertyRelative(nameof(BlendShapeLookAt.right));
				_DrawBlendShape(obj);
			}
		}

		void DrawBlinkProps()
		{
			using (new EditorGUILayout.VerticalScope())
			{
				EditorGUILayout.LabelField("Blink Blendshape");
				_DrawBlendShape(m_BlinkLeftEyeProp);
				_DrawBlendShape(m_BlinkRightEyeProp);
				EditorGUILayout.PropertyField(m_BlinkConfigProp);
			}
		}

		bool _Is(SerializedProperty a, params SerializedProperty[] props)
		{
			for (int i = 0; i < props.Length; ++i)
			{
				if (a.propertyPath.Equals(props[i].propertyPath))
					return true;
			}
			return false;
		}
		bool _Is(SerializedProperty a, SerializedProperty b)
		{
			return a.propertyPath.Equals(b.propertyPath);
		}

		bool _IsBlendShape()
		{
			return m_EyeMethodProp.enumValueIndex == (int)eEyeMethod.BlendShape;
		}

		int _DrawBlendShape(SerializedProperty prop)
		{
			return DrawBlendShape(prop, m_DatabaseProp.objectReferenceValue as FaceRigDatabase);
		}

		public static int DrawBlendShape(SerializedProperty property, FaceRigDatabase db)
		{
			if (db == null)
				return -1;
			db.Editor_Fetch(out string[] names, out int[] indices);
			if (names == null || indices == null)
				return -1;
			var selectedIndex = property.intValue + 1; // 0 == -1 of "None"
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				var val = EditorGUILayout.Popup(property.displayName, selectedIndex, names);
				if (checker.changed)
				{
					property.intValue = indices[val];
					property.serializedObject.ApplyModifiedProperties();
				}
			}

			return property.intValue;
		}
	}
}