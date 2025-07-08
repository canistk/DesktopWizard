using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Kit2.ObjectPool;
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

			DisplayFolder(rootFolder);
			
		}

		[ContextMenu("Test folder")]
		private void Test()
		{
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
		}
	}
}