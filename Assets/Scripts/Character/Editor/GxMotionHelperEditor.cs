using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UniGLTF;
namespace Gaia
{
    [CustomEditor(typeof(GxMotionHelper))]
	public class GxMotionHelperEditor : EditorBase
    {
		private GxMotionHelper self => target as GxMotionHelper;

		private KeyValuePair<bool, string[]> m_ClipNames;// = new KeyValuePair<bool, string[]>(false, null);
		private GxMotionKey[] m_Keys;
		private string[] clips
		{
			get
			{
				if (!m_ClipNames.Key)
				{
					m_Keys = GxMotionDatabase.GetMotions().Select(o => o.Key).ToArray();
					var arr = m_Keys.Select(o =>
					{
						var tag = (o.Type == eAssetType.VRMA) ? "VRMA" : "TL";
						var n = KxPath.GetFileNameWithoutExtension(o.Path);
						return $"[{tag}]:{n}";
					}).ToArray();
					m_ClipNames = new KeyValuePair<bool, string[]>(true, arr);
				}
				return m_ClipNames.Value;
			}
		}
		private string[] m_PoseKeys;
		private string[] poseKeys
		{
			get
			{
				if (m_PoseKeys == null)
				{
					m_PoseKeys = GxMotionDatabase.GetPoses().Select(o => o.key).ToArray();
				}
				return m_PoseKeys;
			}
		}

		private SerializedProperty m_FirstIndexProp;
		private SerializedProperty m_SecondIndexProp, m_PoseIndexProp;
		private SerializedProperty m_FadeInProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_FirstIndexProp = serializedObject.FindProperty(nameof(self.m_FirstIndex));
			m_SecondIndexProp = serializedObject.FindProperty(nameof(self.m_SecondIndex));
			m_PoseIndexProp = serializedObject.FindProperty(nameof(self.m_PoseIndex));
			m_FadeInProp = serializedObject.FindProperty(nameof(self.m_FadeIn));
		}

		private int idx1 { get => m_FirstIndexProp.intValue; set => m_FirstIndexProp.intValue = value; }
		private int idx2 { get => m_SecondIndexProp.intValue; set => m_SecondIndexProp.intValue = value; }
		private int poseIdx { get => m_PoseIndexProp.intValue; set => m_PoseIndexProp.intValue = value; }
		private float fadeIn { get => m_FadeInProp.floatValue; set => m_FadeInProp.floatValue = value; }

		protected override void OnAfterDrawGUI()
		{
			// base.OnAfterDrawGUI();
			if (self.character == null)
			{
				EditorGUILayout.HelpBox("Please assign GxCharacter in this GameObject", MessageType.Warning);
			}

			EditorGUILayout.LabelField("Animation(s)", EditorStyles.boldLabel);
			//using (var scroll = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.ExpandWidth(true)))
			{
				using (var checker = new EditorGUI.ChangeCheckScope())
				{
					var idx = EditorGUILayout.Popup("Motion", idx1, clips, EditorStyles.popup);
					if (checker.changed)
					{
						idx1 = idx;
					}
				}

				using (var checker = new EditorGUI.ChangeCheckScope())
				{
					var idx = EditorGUILayout.Popup("Back loop", idx2, clips, EditorStyles.popup);
					if (checker.changed)
					{
						idx2 = idx;
					}
				}

				if (idx1 >= 0 &&
					idx1 < clips.Length)
				{
					var clip = m_Keys[idx1];
					var clip2 = m_Keys[idx2];
					var cName = (flag ? clip.ShortName : clip2.ShortName);
					if (GUILayout.Button($"Play {cName}", GUILayout.ExpandWidth(true), GUILayout.Height(30f)))
					{
						Debug.Log($"Trigger animation(Editor): {clip.Path}");
						TriggerAnimation(idx1, idx2);
					}
				}
				else
				{
					EditorGUILayout.HelpBox("No animation selected.", MessageType.Info);
				}
			}

			EditorGUILayout.LabelField("Pose(s)", EditorStyles.boldLabel);
			using (var checker = new EditorGUI.ChangeCheckScope())
			{
				var idx = EditorGUILayout.Popup("Pose", poseIdx, poseKeys, EditorStyles.popup);
				if (checker.changed)
				{
					poseIdx = idx;
				}
			}
			if (poseIdx >= 0 && poseIdx < poseKeys.Length)
			{
				var poseKey = poseKeys[poseIdx];
				if (GUILayout.Button($"Change Pose: {poseKey}", GUILayout.ExpandWidth(true), GUILayout.Height(30f)))
				{
					Debug.Log($"Trigger pose change: {poseKey}");
					self.character.ChangePose(poseKey, fadeIn);
				}
			}
			else
			{
				EditorGUILayout.HelpBox("No pose selected.", MessageType.Info);
			}
		}

		private bool flag = false;
		private void TriggerAnimation(int idx, int nextIdx)
		{
			var character = self.character;
			var db = self.data;
			var r1 = db[idx];
			var r2 = db[nextIdx];
			if (character == null || db == null)
			{
				Debug.LogWarning("Character or Database is not assigned or invalid.");
				return;
			}

			var i = flag ? nextIdx : idx;
			self.Editor_AnimationClip(i);
			flag = !flag;
		}
	}
}