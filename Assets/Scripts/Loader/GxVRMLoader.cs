using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniGLTF;
using Unity.VisualScripting;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
    public class GxVRMLoader : MonoBehaviour
    {
        [SerializeField] private string m_Path = $"{Application.streamingAssetsPath}/AliciaSolid_vrm-0.51.vrm";

		[ContextMenu("Load VRM")]
		private void LoadVRM()
        {
            LoadPath(m_Path);
		}

        private async void LoadPath(string path)
        {
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("VRM path is null or empty.");
				return;
			}
			var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                Debug.LogError($"Invaild File {path}");
                return;
            }
			
            // Loaded VRM model
            var vrm = await Vrm10.LoadPathAsync(path, true);
            vrm.transform.SetParent(transform, false);
            
            // post loading for character setup
            var character = vrm.AddComponent<GxCharacter>();
            character.RuntimeCreation();
		}
	}
}