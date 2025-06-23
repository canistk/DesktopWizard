using Kit2;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.AddressableAssets;

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
		ObjectField m_ModeField, m_TPoseField, m_DatabaseField;

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

		private void OnDisable()
		{
			if (m_DetailObjects != null)
			{
				m_DetailObjects = null;
			}

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

			var split = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal)
			{
				style = {
					flexGrow = 1,
					flexDirection = FlexDirection.Row
				}
			};
			split.Add(left);


			var right = VisualElementExtend.SplitVertical(100f, out m_ModelPanel, -1f, out m_GenAniPanel);
			split.Add(right);

			m_ModelPanel.Add(m_ModeField = VisualElementExtend.
				CachePrefabField<GameObject>
				("Human Model", "GxGenerateClip.ModelPrefab"));
			m_ModelPanel.Add(m_TPoseField = VisualElementExtend.
				CachePrefabField<RuntimeAnimatorController>
				("T-Pose", "GxGenerateClip.TPose"));
			m_ModelPanel.Add(m_DatabaseField = VisualElementExtend.
				CachePrefabField<GxTimelineCollection>
				("Database", "GxGenerateClip.collision")
			);

			m_ModelPanel.Add(new Button(OnGenerateAllClicked)
			{
				text = "Generate All Animations",
				style = { width = 200f }
			});


			m_GenAniPanel.style.flexGrow = 1;
			m_GenAniPanel.style.flexDirection = FlexDirection.Column;
			right.Add(m_ModelPanel);
			right.Add(m_GenAniPanel);

			var root = VisualElementExtend.SplitVertical(-1f, out var top, 30f, out var footer);
			top.Add(split);

			SetHint("Completed");
			footer.Add(m_Hints);

			rootVisualElement.Add(root);

		}

		private void OnGenerateAllClicked()
		{
			if (!TryGetModelPath(out var modelPath))
			{
				SetHint("Model prefab is not set.");
				return;
			}

			if (!TryGetTPose(out var tPose))
			{
				SetHint("T-Pose animator is not set.");
				return;
			}
			for (int i = 0; i < s_FullPaths.Length; ++i)
			{
				var path = s_FullPaths[i];
				if (string.IsNullOrEmpty(path))
					continue;

				var fileName = Path.GetFileNameWithoutExtension(path);
				var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
				if (clip == null)
				{
					Debug.LogWarning($"\"{fileName}\" Animation clip not found at path: {path}");
					continue;
				}
				var outputPrefabPath = Path.Combine(s_OutputPath, $"{clip.name}.prefab");
				CreateTimeline(modelPath, tPose, clip, outputPrefabPath);
			}
		}

		private void SetHint(string msg)
		{
			if (m_Hints == null)
			{
				m_Hints = new Label("");
			}
			m_Hints.text = msg;
		}

		private bool TryGetModelPath(out string modelPath)
		{
			var modelRef = m_ModeField.value as GameObject;
			if (modelRef == null)
			{
				modelPath = null;
				return false;
			}
			modelPath = AssetDatabase.GetAssetPath(modelRef);
			return !string.IsNullOrEmpty(modelPath);
		}

		private bool TryGetTPose(out RuntimeAnimatorController tPose)
		{
			tPose = m_TPoseField.value as RuntimeAnimatorController;
			return tPose != null;
		}

		private bool TryGetDatabase(out GxTimelineCollection database)
		{
			database = m_DatabaseField.value as GxTimelineCollection;
			return database != null;
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

				if (!TryGetModelPath(out var modelPath))
				{
					SetHint("Model prefab is not set.");
					return;
				}

				if (!TryGetTPose(out var tPose))
				{
					SetHint("T-Pose animator is not set.");
					return;
				}
				
				foreach (var obj in m_DetailObjects)
				{
					if (obj is AnimationClip clip)
					{
						if (clip.name.Contains("preview", System.StringComparison.OrdinalIgnoreCase))
							continue;
						var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(clip));
						var outputPrefabPath = Path.Combine(s_OutputPath, $"{clip.name}.prefab");
						
						CreateTimeline(modelPath, tPose, clip, outputPrefabPath);


						// var prefab = PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
						Debug.Log($"Generated prefab: {outputPrefabPath}");
					}
				}
				SetHint("Prefab(s) generated successfully.");
			})
			{
				text = "Generate Prefabs"
			};
			m_GenAniPanel.Add(generateButton);
		}

		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
		private void CreateTimeline(
			string modelPath,
			RuntimeAnimatorController tPose,
			AnimationClip clip, string outputPrefabPath)
		{
			
			using (var cp = new CreatePrefab(outputPrefabPath, afterSave: _CovertToAddressable))
			{
				var root = cp.token;
				var timeline = _PrepareTimelineAsset(outputPrefabPath);
				if (timeline == null)
					return;
				
				// Model
				var model = PrefabUtility.LoadPrefabContents(modelPath);
				model.transform.SetParent(root.transform);
				model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

				// timeline + director
				var playableDirector = root.AddComponent<PlayableDirector>();
				playableDirector.playableAsset = timeline;
				playableDirector.playOnAwake = false;

				var track = timeline.CreateTrack<AnimationTrack>("Animation Track");
				_ = track.CreateClip(clip);
				playableDirector.SetGenericBinding(track, model);

				// retargeting
				var retargeting = model.GetOrAddComponent<GxRetargeting>();
				retargeting.animator.runtimeAnimatorController = tPose;
				retargeting.ForceTPose();
				retargeting.animator.runtimeAnimatorController = null;

				// timeline binging
				var GxTimelineAsset = root.AddComponent<GxTimelineAsset>();
				GxTimelineAsset.AssignRetargeting(retargeting);
				GxTimelineAsset.UpdateInfo(clip);
			}

			void _CovertToAddressable(GameObject prefab)
			{
				var settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings == null)
				{
					SetHint("Addressable Asset Settings not found.");
					return;
				}

				var group = settings.groups.FirstOrDefault(g => g.name.Equals("Timeline", IGNORE));
				// var group = settings.groups;
				if (group == null)
				{
					SetHint("Addressable group not found.");
					return;
				}
				var guid = AssetDatabase.AssetPathToGUID(outputPrefabPath);
				var entry = settings.CreateOrMoveEntry(guid, group);
				var fileName = Path.GetFileNameWithoutExtension(outputPrefabPath);
				var address = Path.Combine("Addressable/Timeline", fileName).Replace('\\','/');
				entry.address = address;
				if (TryGetDatabase(out var database))
				{
					var assetRef = new AssetReference(guid);
					database.Add(assetRef, address, clip);
					EditorUtility.SetDirty(database);
				}
			}

			TimelineAsset _PrepareTimelineAsset(string prefabPath)
			{
				// Ready timeline asset
				var dirPath = Path.GetDirectoryName(prefabPath);
				var fileName = Path.GetFileNameWithoutExtension(prefabPath);
				var timelinePath = Path.Combine(dirPath, $"{fileName}_timeline.asset");

				var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
				if (timeline == null)
				{
					AssetDatabase.CreateAsset(new TimelineAsset(), timelinePath);
					AssetDatabase.Refresh();
					timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
					return timeline;
				}
				else
				{
					while (timeline.rootTrackCount > 0)
					{
						var track = timeline.GetRootTrack(0);
						if (track != null)
							timeline.DeleteTrack(track);
					}
				}
				if (timeline == null)
				{
					SetHint($"Failed to create timeline asset at {timelinePath}");
				}
				return timeline;
			}
		}
	}

	public class CreatePrefab : System.IDisposable
	{
		private GameObject _token;
		public GameObject token => _token;
		private string path;
		private System.Action<GameObject> before, after;

		public CreatePrefab(string path,
			System.Action<GameObject> beforeSave = null,
			System.Action<GameObject> afterSave = null)
			: this(path, null, beforeSave, afterSave) { }
		public CreatePrefab(string path, GameObject prefab,
			System.Action<GameObject> beforeSave = null,
			System.Action<GameObject> afterSave = null)
		{
			this._token = prefab == null ?
				new GameObject() : //PrefabUtility.CreateEmptyPrefab()
				(GameObject)PrefabUtility.InstantiatePrefab(prefab);
			this.path = path;
			this.before = beforeSave;
			this.after = afterSave;
		}

		public void Dispose()
		{
			before?.Invoke(_token);
			try
			{
				EditorExtend.EnsureFolderExist(path);
				if (!PrefabUtility.SaveAsPrefabAsset(_token, path, out var success))
					throw new System.Exception("Fail to save as prefab asset.");

				// var prefab = PrefabUtility.LoadPrefabContents(path);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				after?.Invoke(prefab);
			}
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				GameObject.DestroyImmediate(_token);
				_token = null;
			}
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