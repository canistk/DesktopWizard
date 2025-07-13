using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public interface IWinPopupContent
	{
		public void Assign2Popup(GxWinPopup parent);
		public void SelfDespawn();
	}

    public class GxWinPopup : GxWin
    {
		[SerializeField] Canvas m_Canvas;
		public Canvas canvas => m_Canvas;

		private static KeyValuePair<bool, KxObjectPool> s_Pool = default;
		private static KxObjectPool Pool
		{
			get
			{
				if (!s_Pool.Key && s_Pool.Value == null)
				{
					var go = new GameObject("=== GxWinPopup Pool ===");
					go.transform.position = Vector3.zero;
					var pool = go.AddComponent<KxObjectPool>();
					s_Pool = new KeyValuePair<bool, KxObjectPool>(true, pool);
				}
				return s_Pool.Value;
			}
		}

		/// <summary>
		/// creates a popup window at the specified world space position with the given OS space and size.
		/// </summary>
		/// <param name="worldSpace"></param>
		/// <param name="osSpace"></param>
		/// <param name="osSize"></param>
		/// <returns></returns>
		private static GxWinPopup CreateWindow(Vector3 worldSpace, Vector2Int osSpace, Vector2Int osSize)
		{
			const string path = "UIs/DwPopup";
			var go = Pool.Spawn(path, eSrcType.Resources, worldSpace, Quaternion.identity, null, false);
			Debug.Assert(go != null, $"Failed to spawn GxWinPopup from resource '{path}'.");
			var popup = go.GetComponent<GxWinPopup>();
			Debug.Assert(popup != null, $"GxWinPopup component not found in spawned object from resource '{path}'.");
			popup.dwCamera.Width = osSize.x;
			popup.dwCamera.Height = osSize.y;
			popup.dwCamera.Left = osSpace.x;
			popup.dwCamera.Top = osSpace.y;
			return popup;
		}

		/// <summary>
		/// Wrapper for creating and displaying a popup window with a specified prefab.
		/// </summary>
		/// <param name="resourcePath"></param>
		/// <param name="worldSpace"></param>
		/// <param name="osSpace"></param>
		/// <param name="osSize"></param>
		/// <param name="popup"></param>
		/// <param name="contentPage"></param>
		/// <returns></returns>
		public static bool TryDisplay(string resourcePath, Vector3 worldSpace, Vector2Int osSpace, Vector2Int osSize,
			out GxWinPopup popup, out GameObject contentPage, out IWinPopupContent content)
		{
			popup = default;
			contentPage = default;
			content = default;
			if (string.IsNullOrEmpty(resourcePath))
			{
				Debug.LogError("Resource path cannot be null or empty.", null);
				return false;
			}
			try
			{
				popup = CreateWindow(worldSpace, osSpace, osSize);
				contentPage = Pool.Spawn(resourcePath, eSrcType.Resources, popup.canvas.transform, false);
				if (contentPage == null)
					throw new System.Exception($"Prefab '{resourcePath}' not found in Resources.", null);

				if (contentPage.transform is RectTransform rectTransform)
				{
					rectTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					rectTransform.anchorMin = Vector2.zero;
					rectTransform.anchorMax = Vector2.one;
					rectTransform.offsetMin = Vector2.zero;
					rectTransform.offsetMax = Vector2.zero;
					rectTransform.pivot = Vector2.one * 0.5f;
					rectTransform.localScale = Vector3.one;
				}
				else
				{
					contentPage.transform.localPosition = Vector3.zero;
					contentPage.transform.localScale = Vector3.one;
				}

				content = contentPage.GetComponent<IWinPopupContent>();
				if (content == null)
				{
					Debug.LogWarning($"Prefab '{resourcePath}' does not implement IWinPopupContent interface.", null);
				}
				else
				{
					content.Assign2Popup(popup);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Failed to spawn prefab '{resourcePath}': {ex.Message}", null);
				if (contentPage != null)
				{
					Pool.Despawn(contentPage);
					contentPage = null;
				}
				if (popup != null)
				{
					popup.SelfDespawn();
					popup = null;
				}
				return false;
			}
			return popup != null && contentPage != null;
		}
	}
}