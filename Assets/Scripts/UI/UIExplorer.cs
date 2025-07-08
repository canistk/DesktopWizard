using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Kit2.ObjectPool;
using System.Linq;
namespace Gaia
{
    public class UIExplorer : MonoBehaviour
    {
		[SerializeField] UIText m_Title;
		public string Title
		{
			get => m_Title.Text;
			set => m_Title.Text = value;
		}

		[SerializeField] UIControlCollection m_Collection;
		
		private string rootFolder => Application.streamingAssetsPath;

		private void Awake()
		{
			if (!KxDirectory.Exists(rootFolder))
				Debug.LogError($"Folder {rootFolder} not exist.");
			m_Collection.SetSpawnedCallback(OnSpawnedToken);
			m_Collection.SetDespawnCallback(OnDespawnToken);
			DisplayFolder(rootFolder);
		}

		private void DisplayFolder(string path)
		{
			Debug.Assert(Directory.Exists(path), $"path \"{path}\" not exist.");
			List<FileSystemInfo> data = new List<FileSystemInfo>();
			foreach (var dir in KxDirectory.EnumerateDirectories(path))
			{
				Debug.Log($"Folder {dir}");
				DirectoryInfo directoryInfo = new DirectoryInfo(dir);
				data.Add(directoryInfo);
			}

			foreach (var file in KxDirectory.GetFiles(path))
			{
				if (KxPath.IsExtension(file, true, ".meta"))
					continue;
				Debug.Log($"File {file}");
				FileInfo fileInfo = new FileInfo(file);
				data.Add(fileInfo);
			}

			m_Collection.SpawnByDataList(data);

			// Select the first item.
			TrySelectFirstItem();
		}

		private bool TrySelectFirstItem()
		{
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
				// TODO: Load file.
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
				DisplayFolder(data.FullName);
				return;
			}
		}
	}
}