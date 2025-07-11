using DesktopWizard;
using Kit2;
using Kit2.ObjectPool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace Gaia
{
	[MonoSingletonConfig(LoadPath = "UIs/UIPopupCanvas")]
	public class UIPopupCanvas : MonoSingleton<UIPopupCanvas>
	{
		private static readonly Vector3 s_Pos = new Vector3(0f, 100f, -100f);
		private static readonly Quaternion s_Rot = Quaternion.Euler(0f, 180f, 0f);
		[SerializeField] Canvas _canvas;
		[SerializeField] Camera _camera;
		[SerializeField] KxObjectPool _pool;

		private Camera _mainCamera;
		private UniversalAdditionalCameraData _cameraData;

		public Canvas Canvas => _canvas;
		public Camera Camera => _camera;

		[RuntimeInitializeOnLoadMethod]
		public static void Test()
		{
			ReferenceEquals(UIPopupCanvas.Instance, null);

			UIPopup.Info("Test", "This is a test popup canvas.", "OK", () =>
			{
				Debug.Log("Test popup closed.");
			});
		}

		protected override void Awake()
		{
			if (_camera == null || _canvas == null)
				throw new System.NullReferenceException("Invalid prefab setup.");

			base.Awake();

			// avoid overlap Aperion battle ground, assume underground is safe.
			transform.SetPositionAndRotation(s_Pos, s_Rot);

			Setup();
			SceneManager.sceneLoaded += SceneManager_sceneLoaded;
		}
		private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			Setup();
		}
		private void FixedUpdate()
		{
			if (_mainCamera != null)
				return;
			Setup();
		}

		private void Setup()
		{
			var tmpCamera = Camera.main;
			if (tmpCamera == null)
				return;

			var tmp = tmpCamera.GetUniversalAdditionalCameraData();
			if (tmp == null)
				return;
			if (tmp.cameraStack == null)
			{
				// search all camera
				if (tmp.renderType != CameraRenderType.Base)
				{
					Debug.LogWarning($"[PopupCanvas] Invalid Camera {tmp.name}, {tmp.renderType}");
					CompleteSearchMainCamera();
				}
				return; // camera isn't ready yet;
			}
			else
			{
				AssignMainCamera(tmpCamera, tmp);
			}

			_canvas.worldCamera = _camera;
			_camera.gameObject.SetActive(true);
		}

		float m_LastSearch = 0f;
		private void CompleteSearchMainCamera()
		{
			if (Time.unscaledTime - m_LastSearch < 0.2f)
				return;

			m_LastSearch = Time.unscaledTime;
			var cameras = Camera.allCameras;
			for (int i = 0; i < cameras.Length; ++i)
			{
				if (cameras[i] == null)
					continue;
				var cam = cameras[i];
				var cData = cam.GetUniversalAdditionalCameraData();
				if (cData == null)
					continue;
				if (cData.renderType != CameraRenderType.Base)
					continue;
				if (cData.cameraStack == null)
					continue;

				AssignMainCamera(cam, cData);
				Debug.Log($"[PopupCanvas] located main-camera : {cam}", cam);
				return;
			}

			Debug.LogError("[PopupCanvas] fail to search for main camera, skipped.");
		}
		private void AssignMainCamera(Camera cam, UniversalAdditionalCameraData camData)
		{
			this._mainCamera = cam;
			this._cameraData = camData;
			this._cameraData.cameraStack.Add(_camera);
		}

		public (GameObject token, TYPE popup) Spawn<TYPE>(string path, eSrcType type)
			where TYPE : Component
		{
			if (_pool == null)
				throw new System.NullReferenceException("UIPopupCanvas not setup yet.");
			var token = _pool.Spawn(path, type, _canvas.transform, false);
			if (token == null)
			{
				Debug.LogError($"[UIPopupCanvas] Fail to spawn {typeof(TYPE).Name}.");
				return (null, null);
			}
			token.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			if (token.transform is RectTransform rectTran)
			{
				rectTran.localScale = Vector3.one;
				rectTran.pivot = Vector2.one * 0.5f;
				rectTran.anchorMin = Vector2.zero;
				rectTran.anchorMax = Vector2.one;
				rectTran.offsetMin = rectTran.offsetMax = Vector2.zero;
			}
			var popup = token.GetComponent<TYPE>();
			return (token, popup);
		}
	}
}