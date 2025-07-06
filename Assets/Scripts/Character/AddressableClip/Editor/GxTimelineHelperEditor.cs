using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using PlasticPipe.PlasticProtocol.Messages;
using UniVRM10;
using PlasticGui.WebApi.Responses;
using UniGLTF;
namespace Gaia
{
    [CustomEditor(typeof(GxTimelineHelper))]
	public class GxTimelineHelperEditor : EditorBase
    {
		private GxTimelineHelper self => target as GxTimelineHelper;

		private KeyValuePair<bool, string[]> m_ClipNames = default;

		private SerializedProperty m_FirstIndexProp;
		private SerializedProperty m_SecondIndexProp;
		private SerializedProperty m_FadeInProp;
		private SerializedProperty m_VRMAPathProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_FirstIndexProp = serializedObject.FindProperty(nameof(self.m_FirstIndex));
			m_SecondIndexProp = serializedObject.FindProperty(nameof(self.m_SecondIndex));
			m_FadeInProp = serializedObject.FindProperty(nameof(self.m_FadeIn));
			m_VRMAPathProp = serializedObject.FindProperty(nameof(self.m_VRMAPath));
		}

		private int idx1 { get => m_FirstIndexProp.intValue; set => m_FirstIndexProp.intValue = value; }
		private int idx2 { get => m_SecondIndexProp.intValue; set => m_SecondIndexProp.intValue = value; }
		private float fadeIn { get => m_FadeInProp.floatValue; set => m_FadeInProp.floatValue = value; }

		protected override void OnAfterDrawGUI()
		{
			// base.OnAfterDrawGUI();
			if (self.db == null)
			{
				EditorGUILayout.HelpBox("Please assign GxTimelineCollection in Resources", MessageType.Warning);
			}
			if (self.character == null)
			{
				EditorGUILayout.HelpBox("Please assign GxCharacter in this GameObject", MessageType.Warning);
			}

			EditorGUILayout.LabelField("Animation(s)", EditorStyles.boldLabel);
			//using (var scroll = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.ExpandWidth(true)))
			{
				if (!m_ClipNames.Key)
				{
					var clipPath = self.db.Timelines.Select(o => o.addressPath).ToArray();
					m_ClipNames = new KeyValuePair<bool, string[]>(clipPath.Length > 0, clipPath);
				}


				using (var checker = new EditorGUI.ChangeCheckScope())
				{
					var idx = EditorGUILayout.Popup("Timeline", idx1, m_ClipNames.Value, EditorStyles.popup);
					if (checker.changed)
					{
						idx1 = idx;
					}
				}

				using (var checker = new EditorGUI.ChangeCheckScope())
				{
					var idx = EditorGUILayout.Popup("Back loop", idx2, m_ClipNames.Value, EditorStyles.popup);
					if (checker.changed)
					{
						idx2 = idx;
					}
				}

				if (m_ClipNames.Key &&
					idx1 >= 0 &&
					idx1 < m_ClipNames.Value.Length)
				{
					var clip = self.db.Timelines[idx1];
					if (GUILayout.Button("Play Animation", GUILayout.ExpandWidth(true), GUILayout.Height(30f)))
					{
						Debug.Log($"Trigger animation(Editor): {clip.addressPath}");
						TriggerAnimation(idx1, idx2);
					}
				}
				else
				{
					EditorGUILayout.HelpBox("No animation selected.", MessageType.Info);
				}

				if (m_VRMAPathProp != null)
				{
					if (GUILayout.Button("Load VRMA", GUILayout.ExpandWidth(true), GUILayout.Height(30f)))
					{
						Debug.Log($"Trigger VRMA load: {self.m_VRMAPath}");
						self.Editor_LoadVRMA(self.m_VRMAPath);
					}
				}
			}
		}

		private bool flag = false;
		private void TriggerAnimation(int idx, int nextIdx)
		{
			var character = self.character;
			var db = self.db;
			var r1 = db.Timelines[idx];
			var r2 = db.Timelines[nextIdx];
			if (character == null || db == null || r1.assetRef == null)
			{
				Debug.LogWarning("Character or Database is not assigned or invalid.");
				return;
			}

			var i = flag ? nextIdx : idx;
			self.Editor_AnimationClip(i);

			//r.LoadVRMA((vrma) =>
			//{
			//	var vrm = character.GetComponent<Vrm10Instance>();
			//	var glt = character.GetComponent<RuntimeGltfInstance>();
			//	vrm.Runtime.VrmAnimation = vrma;
			//	vrm.Runtime.Process();
			//},
			//Debug.LogError);

		}

		private void TriggerVRMA()
		{
			
		}
	}
}