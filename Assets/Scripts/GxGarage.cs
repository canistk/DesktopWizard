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
			(var token, var popup) = UIPopup.Explorer(Application.streamingAssetsPath, new[] { ".vrm" }, _OnExplorerSelected);
			m_Explorer = popup as UIExplorer;
		}

		UIExplorer m_Explorer;

		private void _OnExplorerSelected(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("Selected path is null or empty.");
				return;
			}
			Debug.Log($"Selected VRM file: {path}");
			GxWinCharacter.LoadVRM(path);
			// m_Explorer.SelfDespawn();
		}
	}
}