using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxGarage : MonoBehaviour
    {
		private void Start()
		{
			// TODO: tutorial flow, on first 2 runs.
			(var token, var popup) = UIPopup.Explorer(GxConst.Path.VRM, new[] { ".vrm" }, _OnExplorerSelected);
			m_Explorer = popup as UIExplorer;
		}

		UIExplorer m_Explorer;

		HashLock<object> m_Loading = new HashLock<object>(true);

		private async void _OnExplorerSelected(string path)
		{
			if (m_Loading.IsLocked)
			{
				Debug.LogWarning($"Loading is already in progress.\n{m_Loading.ToString(detail: true)}");
				return;
			}

			using (m_Loading.AcquireLock(this))
			{
				if (string.IsNullOrEmpty(path))
				{
					Debug.LogError("Selected path is null or empty.");
					return;
				}
				Debug.Log($"Selected VRM file: {path}");
				await GxWinCharacter.LoadVRM(path);
			}
		}

		[ContextMenu("VRM Download Path")]
		private void OpenVRMPath()
		{
			GxConst.Cmd.OpenVRMFolder();
		}


		[ContextMenu("Streaming Assets")]
		private void GotoStreamingAssets()
		{
			GxConst.Cmd.OpenStreamingAssets();
		}
	}
}