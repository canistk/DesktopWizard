using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
namespace Gaia
{
    [CustomEditor(typeof(GxTimelineHelper))]
	public class GxTimelineHelperEditor : EditorBase
    {
		private GxTimelineHelper self => target as GxTimelineHelper;

		private KeyValuePair<bool, string[]> m_ClipNames = default;

		private int m_Index = 0;
		private int m_BaseIndex = 0;

		private float m_FadeIn = 0.2f;
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
			using (var scroll = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.MaxHeight(500f), GUILayout.ExpandWidth(true)))
			{
				if (!m_ClipNames.Key)
				{
					var clipPath = self.db.Timelines.Select(o => o.addressPath).ToArray();
					m_ClipNames = new KeyValuePair<bool, string[]>(clipPath.Length > 0, clipPath);
				}


				using (var checker = new EditorGUI.ChangeCheckScope())
				{
					var idx = EditorGUILayout.Popup("Timeline", m_Index, m_ClipNames.Value, EditorStyles.popup);
					if (checker.changed)
					{
						m_Index = idx;
					}
				}

				using (var checker = new EditorGUI.ChangeCheckScope())
				{
					var idx = EditorGUILayout.Popup("Back loop", m_BaseIndex, m_ClipNames.Value, EditorStyles.popup);
					if (checker.changed)
					{
						m_BaseIndex = idx;
					}
				}

				if (m_ClipNames.Key &&
					m_Index >= 0 &&
					m_Index < m_ClipNames.Value.Length)
				{
					var clip = self.db.Timelines[m_Index];
					if (GUILayout.Button("Play Animation", GUILayout.ExpandWidth(true), GUILayout.Height(30f)))
					{
						Debug.Log($"Trigger animation(Editor): {clip.addressPath}");
						TriggerAnimation(m_Index, m_BaseIndex);
					}
				}
				else
				{
					EditorGUILayout.HelpBox("No animation selected.", MessageType.Info);
				}
			}
		}

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
			character.ClearQueueAnime();
			character.CrossFade(r1.addressPath, m_FadeIn);
			character.QueueAnime(r2.addressPath, m_FadeIn);

		}
	}
}