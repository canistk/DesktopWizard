using Baxter;
using Kit2;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace Gaia
{
    public class GxGenerateClip : EditorWindowBase
    {
		[MenuItem("Gaia/Animations/GxGenerateClip")]
		private static void Init()
		{
			GxGenerateClip window = GetWindow<GxGenerateClip>();
			window.titleContent = new GUIContent("GxGenerateClip - FBX Animation to prefab");
		}

		private const string s_OutputPath= "Assets/Addressable/Timelines";
		string[] s_FullPaths, s_FileNames;
		int m_SelectedIndex = 0;
		VisualElement m_GenAniPanel, m_ModelPanel;
		ObjectField m_ModelField, m_TPoseField, m_DatabaseField;

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

			m_ModelPanel.Add(m_ModelField = VisualElementExtend.
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
			/***
			m_ModelPanel.Add(new Button(TestFunc)
			{
				text = "Test Path define.",
				style = { width = 200f }
			});
			//**/


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

			if (!TryGetAnimator(out var animator))
			{
				SetHint("Animator is not set.");
				// return;
			}

			if (TryGetDatabase(out var database))
			{
				database.Clear();
			}

			GroupDefine(out var single, out var groups);
			var created = false;
			for (int i = 0; i < single.Count; ++i)
			{
				var clipPath	= single[i];
				CreateTimeline(modelPath, animator, tPose, clipPath);
				var dir			= KxPath.GetDirectoryName(clipPath);
				var fileName	= KxPath.GetFileNameWithoutExtension(clipPath);
				var infoPath	= KxPath.Combine(dir, $"{fileName}.asset");
				KxPath.ResolvePath(infoPath, out var abs, out _);
				if (!KxPath.Exists(infoPath))
				{
					var obj = ScriptableObject.CreateInstance<GxTimelineInfo>();
					var list = new List<string>(fileName.ToLower().Split('_'));
					obj.tags = list;
					AssetDatabase.CreateAsset(obj, infoPath);
					created = true;
				}
			}

			/// Assume we already have group of timelines defined in the naming convention.
			/// e.g. "Run_Group", "Run_Loop", "Run_End"
			foreach (var kvp in groups)
			{
				var groupKey = kvp.Key;
				var arr = kvp.Value;
				var converted = new List<string>(arr.Count);
				if (arr == null || arr.Count == 0)
					continue;

				for (int i = 0; i < arr.Count; ++i)
				{
					var clipPath = arr[i];
					var key = CreateTimeline(modelPath, animator, tPose, clipPath);
					converted.Add(key.Path);
				}

				if (converted.Count == 3)
				{
					// we have a complete group, create a Pose data for it.
					var enter = new GxMotionKey(converted[0], eAssetType.Timeline);
					var loop = new GxMotionKey(converted[1], eAssetType.Timeline);
					var exit = new GxMotionKey(converted[2], eAssetType.Timeline);
					var poseData = new GxPoseData(groupKey, enter, loop, exit);
					database.AddPose(poseData); // add to database


					var p = arr[0];
					var dir = KxPath.GetDirectoryName(p);
					var fileName = KxPath.GetFileNameWithoutExtension(p);
					var infoPath = KxPath.Combine(dir, $"{fileName}.asset");
					KxPath.ResolvePath(infoPath, out var abs, out _);
					if (!KxPath.Exists(abs))
					{
						var obj = ScriptableObject.CreateInstance<GxPoseInfo>();
						var list = new List<string>(fileName.ToLower().Split('_'));
						obj.tags = list;
						obj.start = KxPath.GetFileNameWithoutExtension(enter.Path);
						obj.loop = KxPath.GetFileNameWithoutExtension(loop.Path);
						obj.end = KxPath.GetFileNameWithoutExtension(exit.Path);
						AssetDatabase.CreateAsset(obj, infoPath);
					}

				}
				else
				{
					// TODO: may had another cases.
					Debug.LogWarning($"Group '{groupKey}' has {converted.Count} clips, expected 3 (start, loop, end). Skipping Pose creation.");
				}
			}

			if (created)
			{
				AssetDatabase.SaveAssets();
			}
			AssetDatabase.Refresh();
		}

		private void GroupDefine(
			out List<string> single,
			out Dictionary<string, List<string>> groups)
		{
			const int FUZZY_LOGIC = 6;
			single = new List<string>();
			groups = new Dictionary<string, List<string>>();
			var processed = new HashSet<int>();
			if (s_FullPaths == null || s_FullPaths.Length == 0)
			{
				SetHint("No animation clips found.");
				return;
			}
			string[] START = { "start", "begin" };
			string[] LOOP = { "loop", "repeat", "mid" };
			string[] END = { "end", "finish" };
			string[] ANY = END.Concat(LOOP).Concat(START).ToArray();
			char[] SPLITS = { '_', '-', ' ' };
			bool _Is(string val, string[] keywords)
			{
				foreach (var keyword in keywords)
				{
					if (val.Contains(keyword, IGNORE))
						return true;
				}
				return false;
			}
			void _FindPrefixSuffix(string fileName, string[] keywords, out string prefix, out string suffix)
			{
				prefix = string.Empty; suffix = string.Empty;
				if (string.IsNullOrEmpty(fileName) || keywords == null || keywords.Length == 0)
					return;

				var arr = fileName.Split(SPLITS, System.StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i< arr.Length; ++i)
				{
					var word = arr[i];
					if (string.IsNullOrEmpty(word))
						continue;
					if (!_Is(word, keywords))
					{
						prefix += word + "_";
					}
					else
					{
						suffix = string.Join("_", arr.Skip(i + 1));
						if (!string.IsNullOrEmpty(suffix))
							suffix = "_"+suffix; // add a trailing underscore for consistency
						return;
					}
				}
			}

			for (int i = 0; i < s_FullPaths.Length; ++i)
			{
				if (processed.Contains(i))
					continue; // already processed
				var path = s_FullPaths[i];
				if (string.IsNullOrEmpty(path))
					continue;
				var dir = Path.GetDirectoryName(path);
				var fileName = Path.GetFileNameWithoutExtension(path);
				
				// TODO: add specifiy file name check for special cases.

				if (!_Is(fileName, ANY))
				{
					single.Add(path); // single animation clip
					processed.Add(i);
				}
				else
				{
					// Auto search & group `start`, `loop`, `end` clips
					const string GROUP = "Group";
					// group by start, loop, end

					int endFound	= _Is(fileName, END)	? i : -1;
					int loopFound	= _Is(fileName, LOOP)	? i : -1;
					int startFound	= _Is(fileName, START)	? i : -1;

					_FindPrefixSuffix(fileName, ANY, out var prefix, out var suffix);
					string key = $"{prefix}{GROUP}{suffix}"; // use prefix and suffix to form the key
					if (string.IsNullOrEmpty(key))
					{
						processed.Add(i); // mark as processed
						single.Add(path); // single animation clip
						continue; // no group found
					}
					
					for (int k = i + 1; k < s_FullPaths.Length &&
						(startFound == -1 || loopFound == -1 || endFound == -1); ++k)
					{
						if (processed.Contains(k))
							continue; // already processed
						var nextPath = s_FullPaths[k];
						if (string.IsNullOrEmpty(nextPath))
							continue;
						var nextFileName = Path.GetFileNameWithoutExtension(nextPath);

						var startWith = nextFileName.StartsWith(prefix, IGNORE);
						var endsWith = nextFileName.EndsWith(suffix, IGNORE);
						if (!startWith || !endsWith)
							continue; // assume group file with same pattern, skip mismatch.
						
						var factor = StringExtend.LevenshteinDistance(key, nextFileName, false);
						if (factor > FUZZY_LOGIC)
							continue; // skip if the factor is too low, means not a group file.

						if (startFound == -1 && _Is(nextFileName, START))
						{
							startFound = k;
						}
						if (loopFound == -1 && _Is(nextFileName, LOOP))
						{
							loopFound = k;
						}
						if (endFound == -1 && _Is(nextFileName, END))
						{
							endFound = k;
						}
					}
					if (startFound >= 0 && loopFound >= 0 && endFound >= 0)
					{
						// we have a complete group
						if (!groups.TryGetValue(key, out var list))
						{
							list = new List<string>();
							groups.Add(key, list);
						}
						list.Add(s_FullPaths[startFound]);
						list.Add(s_FullPaths[loopFound]);
						list.Add(s_FullPaths[endFound]);
						processed.Add(startFound);
						processed.Add(loopFound);
						processed.Add(endFound);
					}
					else
					{
						single.Add(path); // single animation clip
						processed.Add(i); // mark as processed
					}
				}
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
			var modelRef = m_ModelField.value as GameObject;
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

		private bool TryGetAnimator(out Animator animator)
		{
			animator = null;
			var modelRef = m_ModelField.value as GameObject;
			if (modelRef == null)
				return false;
			animator = modelRef.GetComponent<Animator>();
			return animator != null;
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

				if (!TryGetAnimator(out var animator))
				{
					SetHint("Animator is not set.");
				}
				
				foreach (var obj in m_DetailObjects)
				{
					if (obj is AnimationClip clip)
					{
						if (clip.name.Contains("preview", System.StringComparison.OrdinalIgnoreCase))
							continue;
						var fileName = KxPath.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(clip));
						var outputPrefabPath = Path.Combine(s_OutputPath, $"{clip.name}.prefab").Replace('\\','/');
						var infoPath = KxPath.ChangeExtension(path, ".asset");
						var key = CreateTimeline(modelPath, animator, tPose, clip, outputPrefabPath, infoPath); // manually
						Debug.Log($"Generated prefab: {key}");
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

		private GxMotionKey CreateTimeline(string modelPath, Animator animator, RuntimeAnimatorController tPose, string clipPath)
		{
			if (string.IsNullOrEmpty(clipPath))
				return default;

			var fileName = Path.GetFileNameWithoutExtension(clipPath);
			var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
			if (clip == null)
			{
				Debug.LogWarning($"\"{fileName}\" Animation clip not found at path: {clipPath}");
				return default;
			}

			var infoPath = KxPath.ChangeExtension(clipPath, ".asset");
			// AssetDatabase.LoadAssetAtPath<GxTimelineInfo>(infoPath);

			var outputPrefabPath = KxPath.Combine(s_OutputPath, $"{clip.name}.prefab");
			return CreateTimeline(modelPath, animator, tPose, clip, outputPrefabPath, infoPath); // generate all = single
		}

		/// <summary>
		/// Create Timeline prefab in editor
		/// </summary>
		/// <param name="modelPath">ref base model</param>
		/// <param name="animator"></param>
		/// <param name="tPose">for cache T-Pose reference purpose.</param>
		/// <param name="clip">animation to make timeline.</param>
		/// <param name="outputPrefabPath">export timeline prefab path</param>
		/// <returns></returns>
		private GxMotionKey CreateTimeline(
			string modelPath,
			Animator animator,
			RuntimeAnimatorController tPose,
			AnimationClip clip, string outputPrefabPath, string infoPath)
		{
			//if (animator != null)
			//	ConvertClip2VRMA(animator, clip);

			using (var cp = new CreatePrefab(outputPrefabPath, afterSave: _CovertToAddressable))
			{
				var root = cp.token;
				var timeline = _PrepareTimelineAsset(outputPrefabPath);
				if (timeline == null)
					return default;

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
				var rac = retargeting.animator.runtimeAnimatorController;
				retargeting.animator.runtimeAnimatorController = tPose;
				retargeting.ForceTPose();
				// retargeting.animator.runtimeAnimatorController = rac;
				retargeting.animator.runtimeAnimatorController = null; // remove reference


				if (!TryGetDatabase(out var database))
				{
					Debug.LogError("Fail to get GxTimelineCollection database, please create one first.");
					return default;
				}

				// prepare database record
				var fileName = Path.GetFileName(outputPrefabPath);
				var address = Path.Combine(s_OutputPath, fileName).Replace('\\','/');

				var info = AssetDatabase.LoadAssetAtPath<GxTimelineInfo>(infoPath);
				var record = database.Add(address, clip.isLooping, clip.length, info);

				// timeline binging
				var GxTimelineAsset = root.AddComponent<GxTimelineAsset>();
				GxTimelineAsset.AssignRetargeting(retargeting);
				GxTimelineAsset.UpdateInfo(record, clip);

				EditorUtility.SetDirty(database);
				return record;
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
				var fileName = Path.GetFileName(outputPrefabPath);
				var address = Path.Combine(s_OutputPath, fileName).Replace('\\','/');
				string guid = AssetDatabase.AssetPathToGUID(outputPrefabPath);

				// Check if the entry already exists
				// If it exists, we will update the address and asset reference.
				// If it does not exist, we will create a new entry.
				var check = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(address), true);
				if (check == null)
				{
					var entry = settings.CreateOrMoveEntry(guid, group);
					entry.address = address;
				}
			}

			TimelineAsset _PrepareTimelineAsset(string prefabPath)
			{
				// Ready timeline asset
				var dirPath = Path.GetDirectoryName(prefabPath);
				var fileName = Path.GetFileNameWithoutExtension(prefabPath);
				var timelinePath = Path.Combine(dirPath, $"{fileName}_timeline.asset").Replace('\\','/');

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

		// TODO: Convert AnimationClip to VRMA, make a export function for this.
		// to allow exporting a AnimationClip in to a VRMA file.
		private void ConvertClip2VRMA(Animator animator, AnimationClip clip)
		{
			// if we use ".glb" extension, it will be treated as a GLB file.
			const string EXTENSION = ".vrma";

			var path = KxPath.Combine(s_OutputPath, $"{clip.name}{EXTENSION}");
			EditorExtend.ResolvePath(path, out var absolutePath, out _);
			EditorExtend.EnsureFolderExist(absolutePath);
			/// <see cref="AnimationClipToVrmaAssetCommand.ConvertAnimationClipToVrmAnimation"/>
			var bytes = AnimationClipToVrmaCore.Create(animator, clip);
			File.WriteAllBytes(absolutePath, bytes);

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			var group = settings.groups.FirstOrDefault(g => g.name.Equals("Timeline", IGNORE));
			// var group = settings.groups;
			if (group == null)
			{
				SetHint("Addressable group not found.");
				return;
			}
			var guid = AssetDatabase.AssetPathToGUID(path);
			var entry = settings.CreateOrMoveEntry(guid, group);
			Debug.Log($"VRM Animation saved to: {path}");
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

}