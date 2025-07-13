using DesktopWizard;
using Kit2.ObjectPool;

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
	public abstract class GxWin : MonoBehaviour, ISpawnToken, ISelfDespawnable
	{
		[SerializeField] DwCamera m_DwCamera;

		public uint id => dwForm.id;
		/// <summary>
		/// The camera to render this from U3D to window platform.
		/// </summary>
		public DwCamera dwCamera => m_DwCamera;

		/// <summary>
		/// The System.Windows.Forms.Form that this window is associated with.
		/// </summary>
		public DwForm dwForm => m_DwCamera?.dwForm;

		/// <summary>
		/// The U3D world space window that can represent the coordinates of the form in the monitor.
		/// </summary>
		public DwWindow dwWindow => m_DwCamera?.dwWindow;


		private List<GxWinPart> m_Parts = null;
		public IList<GxWinPart> Parts
		{
			get
			{
				if (m_Parts == null)
				{
					m_Parts = new List<GxWinPart>(GetComponentsInChildren<GxWinPart>());
				}
				return m_Parts;
			}
		}

		public bool TryGetPart<T>(out T part) where T : GxWinPart
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

		protected virtual void Awake()
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
			dwCamera.EVENT_GotFocus += DwCamera_EVENT_GotFocus;
			dwCamera.EVENT_LostFocus += DwCamera_EVENT_LostFocus;
		}

		private void RemoveListener()
		{
			if (dwCamera == null)
				return;
			dwCamera.EVENT_MouseUp -= DwCamera_EVENT_MouseUp;
			dwCamera.EVENT_MouseDown -= DwCamera_EVENT_MouseDown;
			dwCamera.EVENT_MouseMove -= DwCamera_EVENT_MouseMove;
			dwCamera.EVENT_GotFocus += DwCamera_EVENT_GotFocus;
			dwCamera.EVENT_LostFocus += DwCamera_EVENT_LostFocus;
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
		/// <summary>
		/// When the window is enabled, it will try to appear with animation.
		/// search for <see cref="GxAppearHandler"/> in the parts.
		/// </summary>
		/// <param name="completed"></param>
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

		/// <summary>
		/// When the window is disabled, it will try to disappear with animation.
		/// the disappear animation will be called on all <see cref="GxAppearHandler"/> parts.
		/// 
		/// </summary>
		/// <param name="completed"></param>
		public void TryDisappearAni(System.Action completed = null)
		{
			GxAppearHandler m_Helper = null;
			bool found = false;
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
					found = true;
				}
			}
			if (!found)
			{
				// if no GxAppearHandler found, just invoke completed directly.
				gameObject.SetActive(false);
				completed?.Invoke();
				return;
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

		#region Self Despawn
		protected ISpawner m_Spawner;
		public virtual void OnSpawn(ISpawner pool)
		{
			m_Spawner = pool;
		}
		public virtual void OnDespawn()
		{
		}
		public void SelfDespawn()
		{
			if (m_Spawner == null)
				return;
			m_Spawner.Despawn(gameObject);
		}
		#endregion Self Despawn

		#region Focus
		private GxWin m_TranferFocus;
		public void TransferFocus(GxWin win)
		{
			if (win == null)
			{
				m_TranferFocus = null;
				return;
			}
			if (m_TranferFocus == win)
				return;
			m_TranferFocus = win;
		}
		public void StopTransferFocus()
		{
			m_TranferFocus = null;
		}

		private void DwCamera_EVENT_GotFocus()
		{
			if (m_TranferFocus == null)
				return;
			m_TranferFocus.dwForm.Focus();
		}
		private void DwCamera_EVENT_LostFocus() { }
		#endregion Focus
	}

	public enum eSpace
	{
		OS,
		Form,
		Monitor,
		World,
	}
}