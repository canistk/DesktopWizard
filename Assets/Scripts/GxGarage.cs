using Kit2;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Gaia
{
    public class GxGarage : MonoBehaviour
    {
		[SerializeField] private UIButton m_LoadCharacterBtn;
		private void Start()
		{
			if (Screen.fullScreen)
				Screen.fullScreen = false;
			Screen.fullScreenMode = FullScreenMode.Windowed;
			
			// TODO: tutorial flow, on first 2 runs.
			//(var token, var popup) = UIPopup.Explorer(GxConst.Path.VRM, new[] { ".vrm" }, _OnExplorerSelected);
			// m_Explorer = popup as UIExplorer;

			if (m_LoadCharacterBtn)
			{
				m_LoadCharacterBtn.EVENT_OnClick += M_LoadCharacterBtn_EVENT_OnClick;
			}
		}

		KeyValuePair<GxWinPopup, UIExplorer> m_Explorer = default;
		HashLock<object> m_Loading = new HashLock<object>(true);
		private async void M_LoadCharacterBtn_EVENT_OnClick()
		{
			if (m_Explorer.Key != null)
			{
				Debug.LogWarning("Explorer is already open, please close it first.");
				m_Explorer.Key.dwForm.Focus();
				return;
			}
			if (m_Loading.IsLocked)
			{
				Debug.LogWarning($"Loading is already in progress.\n{m_Loading.ToString(detail: true)}");
				return;
			}
			var wPos = new Vector3(0f, 0f, -10f);
			var osPos = new Vector2Int(100, 100);
			var osSize = new Vector2Int(300, 500);
			
			(var popup, var explorer) = await GxWinPopup.Explorer(wPos, osPos, osSize, GxConst.Path.VRM, ".vrm", _OnVRMSelected, autoClose: false);
			m_Explorer = new KeyValuePair<GxWinPopup, UIExplorer>(popup, explorer);
			
			async void _OnVRMSelected(string path)
			{
				if (string.IsNullOrEmpty(path))
				{
					Debug.LogError("Selected path is null or empty.");
					return;
				}
				if (m_Explorer.Value)
				{
					m_Explorer.Value.SelfDespawn();
					m_Explorer = default;
				}
				Debug.Log($"Selected VRM file: {path}");
				using (m_Loading.AcquireLock(this))
				{
					await GxWinCharacter.LoadVRM(path);
				}
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

		[ContextMenu("Test")]
		private void Test()
		{
			if (GxMotionDatabase.Instance == null)
			{
				Debug.Log("Wait for Database loading");
				GxMotionDatabase.EVENT_OnLoaded -= _OnDBLoaded;
				GxMotionDatabase.EVENT_OnLoaded += _OnDBLoaded;
			}
			else
			{
				_OnDBLoaded();
			}

			void _OnDBLoaded()
			{
				GxMotionDatabase.EVENT_OnLoaded -= _OnDBLoaded;
				var db = GxMotionDatabase.Instance;
				Debug.Log($"Database loaded [{db.Count}] ready.");
			}
		}

		[ContextMenu("Test Save")]
		private void TestSave()
		{
			var db = GxMotionDatabase.Instance;
			if (db == null)
			{
				Debug.LogError("Motion database is not loaded.");
				return;
			}
			db.Save();
			Debug.Log("Motion database saved.");
		}
	}
}