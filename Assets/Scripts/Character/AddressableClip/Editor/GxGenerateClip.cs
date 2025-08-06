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
		private static readonly string[] WORK_DIR = new[] { "Assets/Animation" };
		/// <summary>All FBX files under working <see cref="FetchFBXs"/></summary>
		private class FBXInfo
		{
			public string Path;
			public string FileName;

			public FBXInfo(string path)
			{
				this.Path = path;
				this.FileName = System.IO.Path.GetFileNameWithoutExtension(path);
			}

			private string GetInfoPath()
			{
				var dir = KxPath.GetDirectoryName(this.Path);
				if (string.IsNullOrEmpty(dir))
					return string.Empty;
				var infoPath = KxPath.Combine(dir, $"{FileName}.asset");
				return infoPath;
			}

			private bool IsPathExist(string path)
			{
				KxPath.ResolvePath(path, out var abs, out _);
				return KxPath.Exists(abs);
			}

			public bool TryGetClip(out AnimationClip clip)
			{
				clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(this.Path);
				return clip != null;
			}

			public bool IsExist() => IsPathExist(this.Path);

			public bool IsInfoExist() => IsPathExist(GetInfoPath());

			public bool TryGetInfo(out GxMotionData_BuildinDraft obj)
			{
				obj = null;
				var infoPath = GetInfoPath();
				if (!IsPathExist(infoPath))
					return false;
				obj = AssetDatabase.LoadAssetAtPath<GxMotionData_BuildinDraft>(infoPath);
				if (obj is GxPoseDraft poseInfo)
				{
					poseInfo.SetPath(infoPath);
				}
				return obj != null;
			}

			public void GenerateTimelineInfo()
			{
				var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(this.Path);
				if (clip == null)
					return; // skip this
				var infoPath = GetInfoPath();
				if (IsPathExist(infoPath)) throw new System.Exception($"File already exist, {infoPath}");
				var sampleTags = FileName.ToLower().Split('_');
				var obj = ScriptableObject.CreateInstance<GxTimelineDraft>();
				obj.Assign(clip.isLooping, clip.length, sampleTags);
				AssetDatabase.CreateAsset(obj, infoPath);
			}

			public void GeneratePoseInfo(string key, GxTimelineData start, GxTimelineData loop, GxTimelineData exit)
			{
				var infoPath = GetInfoPath();
				if (IsPathExist(infoPath))		throw new System.Exception($"File already exist, {infoPath}");
				
				var sampleTags = FileName.ToLower().Split('_');
				var obj = ScriptableObject.CreateInstance<GxPoseDraft>();
				obj.Assign(key, start, loop, exit, sampleTags);
				
				AssetDatabase.CreateAsset(obj, infoPath);
			}

			public override string ToString()
			{
				return FileName;
			}
		}
		FBXInfo[] s_FBXInfo;
		int m_SelectedIndex = 0;
		VisualElement m_GenAniPanel, m_ModelPanel;
		ObjectField m_ModelField, m_TPoseField, m_DatabaseField;

		private void FetchFBXs(out FBXInfo[] info)
		{
			var guids = AssetDatabase.FindAssets("t:Model", WORK_DIR);
			var cnt = guids.Length;
			info = new FBXInfo[cnt];
			for (int i = 0; i < cnt; ++i)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				info[i] = new FBXInfo(KxPath.Fix(path));
			}
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
			FetchFBXs(out s_FBXInfo);
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
					btn.text = s_FBXInfo[index].FileName;
				},
				itemsSource = s_FBXInfo,
				selectedIndex = m_SelectedIndex,
			};
			left.selectedIndicesChanged += OnFBXSelectionChange;

			var split = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal)
			{
				style = {
					flexGrow = 1,
					flexDirection = FlexDirection.Row
				}
			};
			split.Add(left);


			var right = VisualElementExtend.SplitVertical(150f, out m_ModelPanel, -1f, out m_GenAniPanel);
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

			m_ModelPanel.Add(new Button(Editor_GenerateSampleDataForRawFBXClips)
			{
				text = "Generate sample for raw clips",
				style = { width = 200f }
			});

			m_ModelPanel.Add(new Button(Editor_ConvertFBXClips2Timeline)
			{
				text = "Convert FBX clips to timeline",
				style = { width = 200f }
			});

			m_ModelPanel.Add(new Button(Editor_CollectTimelineInfo)
			{
				text = "Export timeline clips database",
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

		const int FUZZY_LOGIC = 6;
		static readonly string[] START = { "start", "begin" };
		static readonly string[] LOOP = { "loop", "repeat", "mid" };
		static readonly string[] END = { "end", "finish" };
		static readonly char[] SPLITS = { '_', '-', ' ' };
		static readonly string[] ANY = END.Concat(LOOP).Concat(START).ToArray();
		private void Editor_GenerateSampleDataForRawFBXClips()
		{
			var cnt = s_FBXInfo.Length;
			var phase2 = new List<FBXInfo>(8);
			var processed = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
			bool _ContainKeywords(string val, string[] keywords)
			{
				foreach (var keyword in keywords)
				{
					if (val.Contains(keyword, IGNORE))
						return true;
				}
				return false;
			}

			for (int i = 0; i < cnt; ++i)
			{
				var fbx = s_FBXInfo[i];
				if (processed.Contains(fbx.Path))
					continue;
				if (fbx.TryGetInfo(out var info))
				{
					if (info is GxPoseDraft poseInfo)
					{
						processed.Add(poseInfo.start.Path);
						processed.Add(poseInfo.loop.Path);
						processed.Add(poseInfo.end.Path);
					}
					else
					{
						processed.Add(fbx.Path);
					}
					continue; // skip already setup fbx.
				}

				if (!_ContainKeywords(fbx.FileName, ANY))
				{
					fbx.GenerateTimelineInfo();
					processed.Add(fbx.Path);
				}
				else
				{
					phase2.Add(fbx);
				}
			}

			if (phase2.Count == 0)
				return;

			cnt = phase2.Count;
			for (int i = 0; i < cnt; ++i)
			{
				var fbx = phase2[i];
				if (processed.Contains(fbx.Path))
					continue;
				int endFound = _ContainKeywords(fbx.FileName, END) ? i : -1;
				int loopFound = _ContainKeywords(fbx.FileName, LOOP) ? i : -1;
				int startFound = _ContainKeywords(fbx.FileName, START) ? i : -1;

				_FindPrefixSuffix(fbx.FileName, ANY, out var prefix, out var suffix);
				string key = $"{prefix}GROUP{suffix}"; // use prefix and suffix to form the key

				for (int k = i + 1; k < cnt && (startFound == -1 || loopFound == -1 || endFound == -1); ++k)
				{
					if (processed.Contains(phase2[k].Path))
						continue; // already processed
					var next = phase2[k];
					var fName = next.FileName.ToLower();
					if (!fName.StartsWith(prefix, IGNORE) || !fName.EndsWith(suffix, IGNORE))
						continue;
					var factor = StringExtend.LevenshteinDistance(key, fName, false);
					if (factor > FUZZY_LOGIC)
						continue; // skip if the factor is too low, means not a group file.
					if (startFound == -1 && _ContainKeywords(fName, START))	startFound = k;
					if (loopFound == -1 && _ContainKeywords(fName, LOOP))	loopFound = k;
					if (endFound == -1 && _ContainKeywords(fName, END))		endFound = k;
				}
				if (startFound >= 0 && loopFound >= 0 && endFound >= 0)
				{
					// we have a complete group
					var f0 = phase2[startFound];
					var f1 = phase2[loopFound];
					var f2 = phase2[endFound];
					if (!f0.IsExist() || !f1.IsExist() || !f2.IsExist())
					{
						Debug.LogError("Logic error.");
						continue;
					}
					processed.Add(f0.Path);
					processed.Add(f1.Path);
					processed.Add(f2.Path);
					
					var s = AssetDatabase.LoadAssetAtPath<AnimationClip>(f0.Path);
					var l = AssetDatabase.LoadAssetAtPath<AnimationClip>(f1.Path);
					var e = AssetDatabase.LoadAssetAtPath<AnimationClip>(f2.Path);
					var t0 = new GxTimelineData(KxPath.Combine(s_OutputPath, $"{f0.FileName}.prefab"), s.isLooping, s.length);
					var t1 = new GxTimelineData(KxPath.Combine(s_OutputPath, $"{f1.FileName}.prefab"), l.isLooping, l.length);
					var t2 = new GxTimelineData(KxPath.Combine(s_OutputPath, $"{f2.FileName}.prefab"), e.isLooping, e.length);
					fbx.GeneratePoseInfo(key, t0, t1, t2);
				}
				else
				{
					if (processed.Contains(fbx.Path))
						throw new System.Exception("Logic error");
					// this isn't part of pose file, send it to single animation.
					fbx.GenerateTimelineInfo();
					processed.Add(fbx.Path);
				}
			}

			void _FindPrefixSuffix(string fileName, string[] keywords, out string prefix, out string suffix)
			{
				prefix = string.Empty; suffix = string.Empty;
				if (string.IsNullOrEmpty(fileName) || keywords == null || keywords.Length == 0)
					return;

				var arr = fileName.Split(SPLITS, System.StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < arr.Length; ++i)
				{
					var word = arr[i];
					if (string.IsNullOrEmpty(word))
						continue;
					if (!_ContainKeywords(word, keywords))
					{
						prefix += word + "_";
					}
					else
					{
						suffix = string.Join("_", arr.Skip(i + 1));
						if (!string.IsNullOrEmpty(suffix))
							suffix = "_" + suffix; // add a trailing underscore for consistency
						return;
					}
				}
			}

		}

		private string GetExportPath(FBXInfo fbx)
		{
			var fName = fbx.FileName;
			var address = Path.Combine(s_OutputPath, $"{fName}.prefab").Replace('\\', '/');
			return address;
		}

		private void Editor_ConvertFBXClips2Timeline()
		{
			if (!TryGetModelPath(out var modelPath))
				throw new System.Exception("Model Path not found.");
			if (!TryGetTPose(out var tPose))
				throw new System.Exception("TPose reference missing.");
			if (!TryGetAnimator(out var animator))
				throw new System.Exception("Animator reference missing.");

			var cnt = s_FBXInfo.Length;
			for (int i = 0; i < cnt; ++i)
			{
				var fbx = s_FBXInfo[i];
				var clipPath = fbx.Path;
				var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
				if (clip == null)
				{
					var fileName = Path.GetFileNameWithoutExtension(clipPath);
					Debug.LogWarning($"\"{fileName}\" Animation clip not found at path: {clipPath}");
					continue;
				}

				var outputPrefabPath	= GetExportPath(fbx);
				CreateTimeline(modelPath, animator, tPose, clip, outputPrefabPath);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void Editor_CollectTimelineInfo()
		{
			if (!TryGetDatabase(out var database))
				throw new System.Exception("Database reference missing.");

			database.Clear();
			var guids = AssetDatabase.FindAssets($"t:{nameof(GxMotionData_BuildinDraft)}", WORK_DIR);
			var cnt = guids.Length;
			for (int i = 0; i < cnt; ++i)
			{
				var guid = guids[i];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var obj = AssetDatabase.LoadAssetAtPath<GxMotionData_BuildinDraft>(path);

				if (obj is GxTimelineDraft t)
				{
					// assume timeline & asset in same folder.
					var fName = KxPath.GetFileNameWithoutExtension(path);
					var tlPath = KxPath.Combine(s_OutputPath, $"{fName}.prefab");
					KxPath.ResolvePath(tlPath, out var abs, out _);
					if (!KxFile.Exists(tlPath))
					{
						Debug.LogError($"Fail to load related timeline file.\n{tlPath}");
					}
					var info = t.ToData(tlPath);
					database.Add(info);
				}
				else if (obj is GxPoseDraft p)
				{
					var info = p.ToData(s_OutputPath);
					if (database.Add(info))
					{
						database.Add(p.start);
						database.Add(p.loop);
						database.Add(p.end);
					}
				}
				else
				{
					var ex = new System.NotImplementedException($"Type {obj.GetType().Name} not yet defined.");
					Debug.LogError(ex);
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
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

		private void OnFBXSelectionChange(IEnumerable<int> selection)
		{
			if (selection == null || selection.Count() == 0)
			{
				SetHint("Selected: None");
				return;
			}
			var i = selection.ElementAt(0);
			var data = s_FBXInfo[i];
			SetHint($"Selected Path: {data.Path}");
			DetailPage(data);
		}

		Object[] m_DetailObjects;
		private void DetailPage(FBXInfo info)
		{
			m_GenAniPanel.Clear();
			m_DetailObjects = AssetDatabase.LoadAllAssetsAtPath(info.Path);
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

			info.TryGetInfo(out var infoSO);
			m_GenAniPanel.Add(new ObjectField
			{
				value = infoSO,
				label = "Info",
				allowSceneObjects = false,				
			});
		}

		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;

		/// <summary>
		/// Create Timeline prefab in editor
		/// </summary>
		/// <param name="modelPath">ref base model</param>
		/// <param name="animator"></param>
		/// <param name="tPose">for cache T-Pose reference purpose.</param>
		/// <param name="clip">animation to make timeline.</param>
		/// <param name="outputPrefabPath">export timeline prefab path</param>
		/// <returns></returns>
		private void CreateTimeline(
			string modelPath,
			Animator animator,
			RuntimeAnimatorController tPose,
			AnimationClip clip, string outputPrefabPath)
		{
			//if (animator != null)
			//	ConvertClip2VRMA(animator, clip);

			using (var cp = new CreatePrefab(outputPrefabPath, afterSave: _CovertToAddressable))
			{
				var root = cp.token;
				var timeline = _PrepareTimelineAsset(outputPrefabPath);
				if (timeline == null)
				{
					return;
				}

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
					return;
				}

				// prepare database record
				//var fileName = Path.GetFileName(outputPrefabPath);
				//var address = Path.Combine(s_OutputPath, fileName).Replace('\\','/');
				var motionKey = new GxMotionKey(outputPrefabPath, eAssetType.Timeline);

				// timeline binging
				var GxTimelineAsset = root.AddComponent<GxTimelineAsset>();
				GxTimelineAsset.AssignRetargeting(retargeting);
				GxTimelineAsset.UpdateInfo(motionKey, clip);

				EditorUtility.SetDirty(database);
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