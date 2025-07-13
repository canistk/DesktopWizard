using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Kit2.ObjectPool;
using System.Linq;
using Unity.VisualScripting;
namespace Gaia
{
    public class UIExplorer : MonoBehaviour, ISpawnToken, ISelfDespawnable
	{
		[SerializeField] UIText m_Title;
		public string Title
		{
			get => m_Title.Text;
			set => m_Title.Text = value;
		}

		[SerializeField] UIControlCollection m_Collection;
		[SerializeField] UIButton m_BackParentFolder;
		private DirectoryInfo rootFolder;
		private const string VRM = ".vrm";
		private const string VRMA = ".vrma";
		private static readonly string[] VRM_VRMA = new string[]{ VRM, VRMA };
		private void Awake()
		{
			if (!KxDirectory.Exists(Application.streamingAssetsPath))
				Debug.LogError($"Folder {Application.streamingAssetsPath} not exist.");

			if (m_BackParentFolder)
			{
				m_BackParentFolder.EVENT_OnClick += BackParentFolder;
			}

			rootFolder = new DirectoryInfo(Application.streamingAssetsPath);
			m_Collection.SetSpawnedCallback(OnSpawnedToken);
			m_Collection.SetDespawnCallback(OnDespawnToken);
			// Init(rootFolder.FullName, VRM, VRMA);
		}

		private void OnDestroy()
		{
			if (m_BackParentFolder)
			{
				m_BackParentFolder.EVENT_OnClick -= BackParentFolder;
			}
		}

		private System.Action<string> m_FileSelectedCallback;
		public void Init(string path, string[] extension, System.Action<string> fileSelected)
		{
			m_Filter = new FilterInfo(path, extension);
			m_FileSelectedCallback = fileSelected;
			DisplayFolder(m_Filter.rootPath, m_Filter);
		}
		
		private struct FilterInfo
		{
			public string rootPath;
			public string[] extensions;
			public FilterInfo(string root, string[] exts)
			{
				rootPath = root;
				extensions = exts;
			}
		}
		private FilterInfo m_Filter;
		private DirectoryInfo m_CurrentPath = default;
		private void DisplayFolder(string path, in FilterInfo filter)
		{
			if (!Directory.Exists(path))
			{
				Debug.LogError($"path \"{path}\" not exist.");
				return;
			}

			var next = new DirectoryInfo(path);
			var isSubFolder = next.FullName.Length > rootFolder.FullName.Length && path.Substring(0, rootFolder.FullName.Length).Equals(rootFolder.FullName, IGNORE);
			if (m_BackParentFolder)
			{
				m_BackParentFolder.gameObject.SetActive(isSubFolder);
			}

			m_CurrentPath = next;

			List<FileSystemInfo> data = new List<FileSystemInfo>();
			foreach (var dir in KxDirectory.EnumerateDirectories(path))
			{
				Debug.Log($"Folder {dir}");
				DirectoryInfo directoryInfo = new DirectoryInfo(dir);
				data.Add(directoryInfo);
			}

			foreach (var file in KxDirectory.GetFiles(path))
			{
				if (!KxPath.IsExtension(file, true, filter.extensions))
					continue;
				Debug.Log($"File {file}");
				FileInfo fileInfo = new FileInfo(file);
				data.Add(fileInfo);
			}
			m_Collection.SetSorting(_FileSorting);
			m_Collection.SpawnByDataList(data);

			// Select the first item.
			TrySelectFirstItem();
		}

		private int _FileSorting(object a, object b)
		{
			if (a is DirectoryInfo a0 && b is DirectoryInfo b0)
			{
				return a0.Name.CompareTo(b0.Name);
			}
			else if (a is DirectoryInfo a1 && b is FileInfo b1)
			{
				return -1;
			}
			else if (a is FileInfo a2 && b is DirectoryInfo b2)
			{
				return 1;
			}
			else if (a is FileInfo a3 && b is FileInfo b3)
			{
				return a3.Name.CompareTo(b3.Name);
			}
			return 0;
		}

		private bool TrySelectFirstItem()
		{
			if (m_BackParentFolder && m_BackParentFolder.isActiveAndEnabled)
			{
				m_BackParentFolder.button.Select();
				m_BackParentFolder.transform.SetAsFirstSibling();
				return true;
			}
			foreach (var obj in m_Collection.pool.GetSpawnedObjects())
			{
				// assume all prefab had UIButton
				var btn = obj.gameObject.GetComponent<UIButton>();
				if (btn == null)
					continue;
				btn.button.Select();
				return true;
			}
			return false;
		}

		private void OnSpawnedToken(object data, GameObject token)
		{
			var btn = token.GetComponent<UIButton>();
			btn.EVENT_OnClickButton += OnFolderOrFileClicked;
		}

		private void OnDespawnToken(object data, GameObject token)
		{
			var btn = token.GetComponent<UIButton>();
			btn.EVENT_OnClickButton -= OnFolderOrFileClicked;
		}

		private void OnFolderOrFileClicked(UIButton btn)
		{
			Debug.Log($"Clicked {btn.gameObject.name}", btn);
			var fileCtrl = btn.gameObject.GetComponent<UIFileCtrl>();
			if (fileCtrl != null)
			{
				// Load file.
				var path = fileCtrl.data.FullName;
				if (!KxFile.Exists(path))
					throw new System.Exception($"Path {path}, not exist.");
				var ext = KxPath.GetExtension(path);
				if (string.IsNullOrEmpty(ext))
					throw new System.Exception($"Unknown file type, extension not found.");

				m_FileSelectedCallback?.Invoke(path);
				// TryExecuteFile(fileCtrl.data);
				return;
			}


			var folderCtrl = btn.gameObject.GetComponent<UIFolderCtrl>();
			if (folderCtrl != null)
			{
				// Display folder
				var data = folderCtrl.data;
				var path = data.FullName;
				if (!KxDirectory.Exists(path))
					Debug.LogError($"invalid path {path}");
				DisplayFolder(data.FullName, m_Filter);
				return;
			}
		}

		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
		private void BackParentFolder()
		{
			if (!m_CurrentPath.Exists)
				return;

			var back = m_CurrentPath.Parent.FullName;
			DisplayFolder(back, m_Filter);
		}

		private bool TryExecuteFile(FileInfo data)
		{
			var path = KxPath.Fix(data.FullName);

			try
			{
				if (!KxFile.Exists(path))
					throw new System.Exception($"Path {path}, not exist.");
				var ext = KxPath.GetExtension(path);
				if (string.IsNullOrEmpty(ext))
					throw new System.Exception($"Unknown file type, extension not found.");
				switch (ext)
				{
					case VRM: LoadVRM(path); break;
					case VRMA: LoadVRMA(path); break;
					default: throw new System.Exception($"Unknown file type, extension \"{ext}\"");
				}
				return true;
			}
			catch (System.Exception ex)
			{
				Debug.LogError(ex);
				return false;
			}
		}

		private bool TryReadText(FileInfo data, out string content)
		{
			var path = KxPath.Fix(data.FullName);
			if (!KxFile.Exists(path))
			{
				content = default;
				return false;
			}

			try
			{
				using (var reader = data.OpenText())
				{
					content = reader.ReadToEnd();
					return true;
				}
			}
			catch (System.Exception ex)
			{
				content = ex.Message;
				return false;
			}
		}

		private GxCharacter m_LastCharacter;
		private int m_SpawnedCount = 0;
		private Dictionary<GxCharacter, GxWin> m_CharacterDict = new Dictionary<GxCharacter, GxWin>();
		[System.Obsolete("Moved to GxModelView.LoadVRM")]
		private void LoadVRM(string path)
		{
			GxWinCharacter.LoadVRM(path);

			Debug.Log($"Loading VRM {path}");
			GxVRMLoader.LoadModel(path, (ch, vrm) =>
			{
				var _name = KxPath.GetFileNameWithoutExtension(path);
				Debug.Log($"Loaded {_name}", ch);
				m_LastCharacter = ch;
				try
				{
					Debug.Log($"Regist desktop wizard {_name}");
					var prefab = Resources.Load("DWTemplate");
					var pos = new Vector3(0f, 3f * m_SpawnedCount, 0f);
					var token = GameObject.Instantiate(prefab, pos, Quaternion.identity);
					token.name = $"MV-{_name}";
					var modelView = token.GetComponentInChildren<GxWinCharacter>();
					modelView.Assign(ch);
					++m_SpawnedCount;
					m_CharacterDict.Add(ch, modelView);
					modelView.dwForm.FormClosed += (sender, e) =>
					{
						UnloadVRM(ch, modelView);
					};
				}
				catch (System.Exception ex)
				{
					throw ex;
				}
			}, Debug.LogException);
		}
		[System.Obsolete("Moved to GxModelView.LoadVRM")]
		private void UnloadVRM(GxCharacter character, GxWin modelView)
		{
			Debug.Log($"Desktop wizard closed {character.name}");
			if (m_CharacterDict.ContainsKey(character))
			{
				m_CharacterDict.Remove(character);
			}
			else
			{
				Debug.LogWarning($"Character {character.name} not found in dictionary.");
			}
			GxVRMLoader.UnloadModel(character);
			GameObject.Destroy(modelView.gameObject);
		}

		private void LoadVRMA(string path)
		{
			Debug.Log($"Loading VRMA {path}");
			if (m_LastCharacter == null)
			{
				Debug.LogError("Character not loaded, cannot load VRMA.");
				return;
			}

			m_LastCharacter.CrossFade(path, 0.25f, eSrcType.GameObject, false);
		}

		#region ISpawnToken
		private ISpawner m_Spawner;

		public void OnSpawn(ISpawner pool)
		{
			this.m_Spawner = pool;
		}

		public virtual void SelfDespawn()
		{
			if (m_Spawner != null)
			{
				m_Spawner.Despawn(gameObject);
				m_Spawner = null;
			}
		}
		public virtual void OnDespawn()
		{
		}
		#endregion ISpawnToken

		[ContextMenu("Head to Streaming Assets")]
		private void GotoStreamingAssets()
		{
			Kit2.Platform.CommandLine("explorer.exe", KxPath.Fix(Application.streamingAssetsPath), (feedback) =>
			{
				Debug.Log(feedback);
			});
		}
	}
}