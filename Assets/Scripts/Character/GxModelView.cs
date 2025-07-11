using DesktopWizard;
//using Kit2;
using Kit2.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Gaia
{
    public class GxModelView : MonoBehaviour
    {
        [SerializeField] DwCamera m_DwCamera;

        [SerializeField] Transform m_CharacterPivot;

		public uint id => dwForm.id;
        public DwCamera dwCamera => m_DwCamera;
        public DwForm dwForm => m_DwCamera?.dwForm;
		
		/****
		// another way to get DwWindow
		if (DwCore.instance.TryGetOS(out var os) &&
			os.TryGetWindowById(this.id, out var win))
		{
		}
		// ****/
		public DwWindow dwWindow => m_DwCamera?.dwWindow;


		private KeyValuePair<bool, GxCharacter> m_Character;
		/// <summary>Try get character component in this model view. (nullable)</summary>
		/// </summary>
		public GxCharacter Character
		{
			get
			{
				if (!m_Character.Key)
				{
					m_Character = new KeyValuePair<bool, GxCharacter>(true, GetComponentInChildren<GxCharacter>(true));
				}
				return m_Character.Value;
			}
		}
		public void Assign(GxCharacter character)
		{
			if (m_Character.Value != null)
			{
				Debug.LogError($"Already assigned character {m_Character.Value.name}", m_Character.Value);
				return;
			}
			
			// Align  ModelView, but parent pivot (due to camera orbit control)
			character.transform.SetParent(m_CharacterPivot, false);
			character.transform.SetPositionAndRotation(transform.position, transform.rotation);

			m_Character = new KeyValuePair<bool, GxCharacter>(true, character);
		}

		private KeyValuePair<bool, GxCameraCtrl> m_CameraCtrl;
		public GxCameraCtrl CameraCtrl
		{
			get
			{
				if (!m_CameraCtrl.Key)
				{
					m_CameraCtrl = new KeyValuePair<bool, GxCameraCtrl>(true, GetComponentInChildren<GxCameraCtrl>(true));
				}
				return m_CameraCtrl.Value;
			}
		}

		private List<GxModelPart> m_Parts = null;
		public IList<GxModelPart> Parts
		{
			get
			{
				if (m_Parts == null)
				{
					m_Parts = new List<GxModelPart>(GetComponentsInChildren<GxModelPart>());
				}
				return m_Parts;
			}
		}

		public bool TryGetPart<T>(out T part) where T : GxModelPart
		{
			foreach (var p in Parts)
			{
				if (p is not T t)
					continue;
				part = t;
				return true;
			}
			part = null;
			return false;
		}

		private void Reset()
		{
			m_DwCamera = GetComponentInChildren<DwCamera>(true);
		}

		private void Awake()
		{
			ReferenceEquals(Character, null); // Force initialization of Character
			AddListener();
		}

		private void OnDestroy()
		{
			RemoveListener();
		}

		protected virtual void OnEnable()
		{
			InitTasks();
			TryAppearAni();
		}

		protected virtual void OnDisable()
		{
			DeinitTasks();
		}

		#region Mouse Control

		private List<IPointerFeature> m_Features;
		private List<IPointerFeature> features
		{
			get
			{
				if (m_Features == null)
				{
					m_Features = new List<IPointerFeature>(8);
				}
				return m_Features;
			}
		}
		public void Register(IPointerFeature feature)
		{
			if (features.Contains(feature))
			{
				Debug.LogError("Duplicate feature detected:");
				return;
			}
			features.Add(feature);
		}
		public bool Unregister(IPointerFeature feature)
		{
			return features.Remove(feature);
		}

		private void AddListener()
		{
			if (dwCamera == null)
				return;
			dwCamera.EVENT_MouseUp += DwCamera_EVENT_MouseUp;
			dwCamera.EVENT_MouseDown += DwCamera_EVENT_MouseDown;
			dwCamera.EVENT_MouseMove += DwCamera_EVENT_MouseMove;
		}

		private void RemoveListener()
		{
			if (dwCamera == null)
				return;
			dwCamera.EVENT_MouseUp -= DwCamera_EVENT_MouseUp;
			dwCamera.EVENT_MouseDown -= DwCamera_EVENT_MouseDown;
			dwCamera.EVENT_MouseMove -= DwCamera_EVENT_MouseMove;
		}

		private void DwCamera_EVENT_MouseMove(PointerEventData evt)
		{
			var i = features.Count;
			while (i-- > 0)
			{
				if (features[i] == null)
				{
					features.RemoveAt(i);
					continue;
				}
				if (!features[i].isActive)
					continue;
				features[i].MouseMove(this, evt);
			}
		}

		private void DwCamera_EVENT_MouseDown(PointerEventData evt)
		{
			var i = features.Count;
			while (i-- > 0)
			{
				if (features[i] == null)
				{
					features.RemoveAt(i);
					continue;
				}
				if (!features[i].isActive)
					continue;
				features[i].MouseDown(this, evt);
			}
		}

		private void DwCamera_EVENT_MouseUp(PointerEventData evt)
		{
			var i = features.Count;
			while (i-- > 0)
			{
				if (features[i] == null)
				{
					features.RemoveAt(i);
					continue;
				}
				if (!features[i].isActive)
					continue;
				features[i].MouseUp(this, evt);
			}
		}
		#endregion Mouse Control

		#region Appear/Disappear
		public void TryAppearAni(System.Action completed = null)
		{
			GxAppearHandler m_Helper = null;
			foreach (var p in Parts)
			{
				if (p is not GxAppearHandler helper)
					continue;
				if (helper.gameObject.activeSelf)
				{
					if (helper.state == GxAppearHandler.eState.Invalid ||
						helper.state >= GxAppearHandler.eState.Disappearing)
					{
						if (completed != null)
						{
							m_Helper = helper;
							m_Helper.EVENT_StateChanged += _OnAppearEnd;
						}
						helper.Appear();
					}
				}
			}
			void _OnAppearEnd(GxAppearHandler.eState state)
			{
				if (state != GxAppearHandler.eState.Appeared)
					return;
				m_Helper.EVENT_StateChanged -= _OnAppearEnd;
				completed?.Invoke();
			}
		}

		public void TryDisappearAni(System.Action completed = null)
		{
			GxAppearHandler m_Helper = null;
			foreach (var p in Parts)
			{
				if (p is not GxAppearHandler helper)
					continue;
				if (helper.gameObject.activeSelf)
				{
					if (completed != null)
					{
						m_Helper = helper;
						m_Helper.EVENT_StateChanged += _OnDisappearEnd;
					}
					helper.Disappear();
				}
			}
			void _OnDisappearEnd(GxAppearHandler.eState state)
			{
				if (state != GxAppearHandler.eState.Disappeared)
					return;
				m_Helper.EVENT_StateChanged -= _OnDisappearEnd;
				completed?.Invoke();
			}
		}
		#endregion Appear/Disappear

		public void MoveTo(float x, float y, eSpace space)
		{
			if (dwCamera == null)
				return;
			switch (space)
			{
				case eSpace.OS:
				if (dwForm == null)
				{
					Debug.LogError("dwForm is null");
					return;
				}
				dwForm.MoveTo_OS((int)x, (int)y);
				break;


				case eSpace.Monitor:
				if (dwWindow == null)
				{
					Debug.LogError("dwWindow is null");
					return;
				}
				dwWindow.MoveTo_Monitor(x, y);
				break;

				case eSpace.Form:
				case eSpace.World:
				default:
				throw new System.NotImplementedException();
			}
		}

		#region Movment

		private List<MyTaskBase> m_Tasks = null;
		private void InitTasks()
		{
			if (m_Tasks == null)
				m_Tasks = new List<MyTaskBase>(8);
			m_Tasks.Clear();
		}

		private void DeinitTasks()
		{
			if (m_Tasks == null)
				return;
			for (int i = 0; i < m_Tasks.Count; ++i)
			{
				if (m_Tasks[i] == null)
					continue;
				if (m_Tasks[i] is not MyTask t)
					continue;
				try
				{
					t.Abort();
				}
				catch (System.Exception ex)
				{
					Debug.LogException(ex);
				}
			}
			m_Tasks.Clear();
		}
		#endregion Movement

		#region Runtime Creation
		private static Dictionary<GxCharacter, GxModelView> s_CharacterDict = new Dictionary<GxCharacter, GxModelView>();
		private static int s_SpawnedVRMCount = 0;
		public static void LoadVRM(string vrmFilePath)
		{
			if (string.IsNullOrEmpty(vrmFilePath))
			{
				Debug.LogError("VRM file path is null or empty.");
				return;
			}

			Debug.Log($"Loading VRM : {vrmFilePath}");
			GxVRMLoader.LoadModel(vrmFilePath, (ch, vrm) =>
			{
				var _name = Kit2.KxPath.GetFileNameWithoutExtension(vrmFilePath);
				Debug.Log($"Loaded {_name}", ch);
				try
				{
					Debug.Log($"Regist desktop wizard {_name}");
					var prefab = Resources.Load("DWTemplate");
					var pos = new Vector3(0f, 3f * s_SpawnedVRMCount, 0f);
					var token = (GameObject)GameObject.Instantiate(prefab, pos, Quaternion.identity);
					token.name = $"MV-{_name}";
					var modelView = token.GetComponentInChildren<GxModelView>();
					modelView.Assign(ch);
					++s_SpawnedVRMCount;
					s_CharacterDict.Add(ch, modelView);
					modelView.dwForm.FormClosed += (sender, e) =>
					{
						UnloadVRM(ch);
					};
					modelView.dwCamera.EVENT_MouseDown += DwCamera_EVENT_MouseRightClicked;
				}
				catch (System.Exception ex)
				{
					throw ex;
				}
			}, Debug.LogException);
		}

		private static void DwCamera_EVENT_MouseRightClicked(PointerEventData evt)
		{
			// check is right click
			if (evt.pointerId != 2)
			{
				return;
			}
			Debug.LogWarning("Right clicked.");
		}

		private static bool UnloadVRM(GxCharacter character)
		{
			if (!s_CharacterDict.TryGetValue(character, out var modelView))
			{
				Debug.LogWarning($"Character {character.name} not found in model view dictionary.");
				return false;
			}

			try
			{
				s_CharacterDict.Remove(character);
				GxVRMLoader.UnloadModel(character);
				GameObject.Destroy(modelView.gameObject);
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Failed to unload VRM for character {character.name}: {ex.Message}");
				return false;
			}
			return true;
		}


		private static UIPopupCharacterMenu m_CharacterMenu;
		public static UIPopupCharacterMenu DisplayCharacterMenu(int Left, int Top, GxModelView modelView, GxCharacter character)
		{
			if (m_CharacterMenu == null)
			{
				var prefab = Resources.Load("UIs/UIPopupCharacterMenu");
				var pos = new Vector3(0f, 3f * s_SpawnedVRMCount, 0f);
				var token = (GameObject)GameObject.Instantiate(prefab, pos, Quaternion.identity);
				m_CharacterMenu = token.GetComponentInChildren<UIPopupCharacterMenu>();
				if (m_CharacterMenu == null)
					throw new System.NullReferenceException("UIPopupCharacterMenu not found on prefab.");
			}
			m_CharacterMenu.Init(Left, Top, modelView, character);
			return m_CharacterMenu;
		}
		#endregion Runtime Creation
	}

	public enum eSpace
	{
		OS,
		Form,
		Monitor,
		World,
	}

	public abstract class GxAppearHandler : GxModelPart
	{
		public enum eState
		{
			Invalid = 0,
			Appearing = 1,
			Appeared = 2,
			Disappearing = 10,
			Disappeared = 11,
		}

		public event System.Action<eState> EVENT_StateChanged;
		private eState m_State = eState.Invalid;
		public eState state
		{
			get => m_State;
			set
			{
				if (m_State == value)
					return;
				switch (value)
				{
					case eState.Invalid:
					break;
					case eState.Appearing:
					if (m_State == eState.Appeared)	throw new System.Exception();
					if (m_State == eState.Disappearing) _EndDisappeared();
					_StartAppearing();
					break;
					case eState.Appeared:
					if (m_State == eState.Disappearing) _EndDisappeared();
					if (m_State == eState.Disappeared) _StartAppearing();
					EndAppeared();
					break;
					case eState.Disappearing:
					if (m_State == eState.Disappeared) throw new System.Exception();
					if (m_State == eState.Appearing) EndAppeared();
					StartDisappearing();
					break;
					case eState.Disappeared:
					if (m_State == eState.Appearing) EndAppeared();
					if (m_State == eState.Appeared) StartDisappearing();
					_EndDisappeared();
					break;
					default:
					throw new System.NotImplementedException();
				}
				m_State = value;
				EVENT_StateChanged.TryCatchDispatchEventError(o => o.Invoke(m_State));
			}
		}

		private void Update()
		{
			if (state == eState.Appearing)
			{
				var alive = InternalAppearing();
				if (!alive)
					state = eState.Appeared;
			}
			if (state == eState.Disappearing)
			{
				var alive = InternalDisappearing();
				if (!alive)
					state = eState.Disappeared;
			}
		}

		[ContextMenu("Appear")]
		public void Appear()
		{
			if (state == eState.Disappeared ||
				state == eState.Invalid)
			{
				state = eState.Appearing;
			}
		}

		[ContextMenu("Disappear")]
		public void Disappear()
		{
			if (state != eState.Appeared)
				return;
			state = eState.Disappearing;
		}

		private void _StartAppearing()
		{
			StartAppearing();
		}
		protected virtual void StartAppearing() { }
		protected abstract bool InternalAppearing();
		protected virtual void EndAppeared() { }

		protected virtual void StartDisappearing() { }
		protected abstract bool InternalDisappearing();
		private void _EndDisappeared()
		{
			EndDisappeared();
		}
		protected virtual void EndDisappeared() { }
	}
}