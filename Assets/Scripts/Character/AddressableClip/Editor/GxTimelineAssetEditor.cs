using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kit2;
namespace Gaia
{
	[CustomEditor(typeof(GxTimelineAsset))]
	public class GxTimelineAssetEditor : EditorBase
    {
		private GxTimelineAsset self => target as GxTimelineAsset;
		SerializedProperty isLoopProp, durationProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			
			isLoopProp = serializedObject.FindProperty("m_IsLoop");
			durationProp = serializedObject.FindProperty("m_Duration");
		}
		protected override void OnDrawProperty(SerializedProperty property)
		{
			if (property.propertyPath == isLoopProp.propertyPath ||
				property.propertyPath == durationProp.propertyPath)
			{
				using (new EditorGUI.DisabledGroupScope(true))
				{
					// EditorGUILayout.PropertyField(property, includeChildren: true);
					base.OnDrawProperty(property);
				}
			}
			else
			{
				base.OnDrawProperty(property);
			}
		}

		protected override void OnAfterDrawGUI()
		{
			if (GUILayout.Button("Toggle Renderers", GUILayout.ExpandWidth(true), GUILayout.Height(30f)))
			{
				if (!Application.isPlaying)
				{
					EditorUtility.DisplayDialog("Warning", "Toggle Renderers only works in Play Mode.", "OK");
					return;
				}
				self.ToggleRenerer();
			}
		}

	}
}