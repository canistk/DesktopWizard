using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
//using Kit2;
using Kit2.Tasks;
namespace Gaia
{
    public class GxModelView : MonoBehaviour
    {
        [SerializeField] DwCamera m_DwCamera;
        [SerializeField] BodyLayout m_BodyLayout;

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

		public BodyLayout bodyLayout
        {
            get
            {
                if (m_BodyLayout == null)
                    m_BodyLayout = GetComponent<BodyLayout>();
                return m_BodyLayout;
            }
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
            m_BodyLayout = GetComponentInChildren<BodyLayout>(true);
		}

		private void Awake()
		{
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