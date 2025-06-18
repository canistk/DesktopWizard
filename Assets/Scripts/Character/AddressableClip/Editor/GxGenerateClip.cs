using Kit2;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Gaia
{
    public class GxGenerateClip : EditorWindowBase
    {
		[MenuItem("Gaia/Animations/GxGenerateClip")]
		private static void Init()
		{
			GxGenerateClip window = GetWindow<GxGenerateClip>();
			window.titleContent = new GUIContent("FBX Animation to prefab");
		}

		private const string s_OutputPath= "Assets/Addressable/Timelines";
		string[] s_FullPaths, s_FileNames;
		int m_SelectedIndex = 0;
		VisualElement m_GenAniPanel, m_ModelPanel;
		private void FetchFiles()
		{
			var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Animation" });
			var paths = new List<string>(guids.Length);
			var fileNames = new List<string>(guids.Length);
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				paths.Add(path);
				var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
				fileNames.Add(fileName);
			}
			s_FullPaths = paths.ToArray();
			s_FileNames = fileNames.ToArray();
		}

		Label m_Hints;
		public void CreateGUI()
		{
			FetchFiles();
			var left = new ListView
			{
				style = {
					flexGrow = 1,
					width = 300f,
					marginRight = 10,
					flexDirection = FlexDirection.Column | FlexDirection.Row
				},
				makeItem = () => new Label(),
				bindItem = (o, index) =>
				{
					var btn = o as Label;
					btn.text = s_FileNames[index];
				},
				itemsSource = s_FileNames,
				selectedIndex = m_SelectedIndex,
			};
			left.selectedIndicesChanged += OnAniSelectionChange;

			m_ModelPanel = new VisualElement()
			{
				style = {
					flexGrow = 1,
					flexDirection = FlexDirection.Column,
				}
			};
			m_GenAniPanel = new VisualElement()
			{
				style = {
					flexGrow = 1,
					flexDirection = FlexDirection.Column,
					display = DisplayStyle.Flex,
				}
			};
			var split = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal)
			{
				style = {
					flexGrow = 1,
					flexDirection = FlexDirection.Row
				}
			};
			split.Add(left);
			var detail = VisualElementExtend.SplitVertical(300f, out m_ModelPanel, -1f, out m_GenAniPanel);
			m_ModelPanel.style.flexGrow = 1;
			m_ModelPanel.style.flexDirection = FlexDirection.Column;
			m_GenAniPanel.style.flexGrow = 1;
			m_GenAniPanel.style.flexDirection = FlexDirection.Column;
			
			split.Add(m_GenAniPanel);

			var root = VisualElementExtend.SplitVertical(-1f, out var top, 30f, out var footer);
			top.Add(split);

			SetHint("Completed");
			footer.Add(m_Hints);

			rootVisualElement.Add(root);

		}

		private void SetHint(string msg)
		{
			if (m_Hints == null)
			{
				m_Hints = new Label("");
			}
			m_Hints.text = msg;
		}

		private void OnAniSelectionChange(IEnumerable<int> selection)
		{
			if (selection == null || selection.Count() == 0)
			{
				SetHint("Selected: None");
				return;
			}
			var i = selection.ElementAt(0);
			var path = s_FullPaths[i];
			SetHint($"Selected: {path}");
			DetailPage(path);
		}

		Object[] m_DetailObjects;
		private void DetailPage(string path)
		{
			m_GenAniPanel.Clear();
			m_DetailObjects = AssetDatabase.LoadAllAssetsAtPath(path);

			var rootGO = m_DetailObjects.FirstOrDefault(o => o is GameObject) as GameObject;

			for (int i = 0; i < m_DetailObjects.Length; ++i)
			{
				var obj = m_DetailObjects[i];
				if (obj is Transform)
					continue;
				if (obj is GameObject && obj != rootGO)
					continue; // skip non-root gameobject, since it is the model itself.
				if (obj is not AnimationClip clip)
					continue;
				if (clip.name.Contains("preview"))
					continue; // skip preview clips
				var field = new ObjectField
				{
					value = obj,
					label = $"[{i:00}]{obj.GetType().Name}",
					allowSceneObjects = false
				};
				m_GenAniPanel.Add(field);
			}

			var generateButton = new Button(() =>
			{
				if (m_DetailObjects == null || m_DetailObjects.Length == 0)
				{
					SetHint("No animation clips found.");
					return;
				}


				foreach (var obj in m_DetailObjects)
				{
					if (obj is AnimationClip clip)
					{
						var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(clip));
						var prefabPath = Path.Combine(s_OutputPath, $"{fileName}.prefab");
						
						CreateTimeline(clip, prefabPath);


						// var prefab = PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
						Debug.Log($"Generated prefab: {prefabPath}");
					}
				}
				SetHint("Prefab(s) generated successfully.");
			})
			{
				text = "Generate Prefabs"
			};
			m_GenAniPanel.Add(generateButton);
		}
		
		private void CreateTimeline(AnimationClip clip, string path)
		{
			EditorExtend.EnsureFolderExist(path);
		}
	}


	public static class GenerateUtils
	{
		public static void GenerateTimeline(string modelPath, string animationPath)
		{
			var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
			var ani = AssetDatabase.LoadAssetAtPath<GameObject>(animationPath);
		}
	}
}