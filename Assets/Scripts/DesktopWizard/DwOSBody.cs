using Kit2;
using Kit2.Task;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DesktopWizard
{
    public class DwOSBody : MonoBehaviour
    {
		private static DwOSBody m_Instance = null;

		public static DwOSBody rawInstance => m_Instance;
		public static DwOSBody instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = FindObjectOfType<DwOSBody>();
					if (m_Instance == null)
					{
						var go = new GameObject("DwOSBody", typeof(DwOSBody));
						// m_Instance = go.AddComponent<DwOSBody>();
					}
				}
				return m_Instance;
			}
		}

		[SerializeField]
		private DwCore.LayerInfo m_LayerInfo = new DwCore.LayerInfo()
		{
			m_ScreenLayer = "Screen",
			m_RawBorderLayer = "RawScreenBorder",
			m_BorderLayer = "ScreenBorder",
			m_WindowLayer = "Window",
		};

		[Header("Screen")]
		[SerializeField] ScreenHandler.Setting m_ScreenSetting = new ScreenHandler.Setting();
		[SerializeField] UpdateInfo m_ScreenUpdate = new UpdateInfo(5f);
		ScreenHandler m_UpdateScreen;

		[Header("Window")]
		[SerializeField] WindowHandler.Setting m_WindowSetting = new WindowHandler.Setting();
		[SerializeField] UpdateInfo m_WindowUpdate = new UpdateInfo(0.1f);
		WindowHandler m_UpdateWindow;

		[System.Serializable]
		private struct UpdateInfo
		{
			public float lastUpdate { get; private set; }
			public float interval;
			public UpdateInfo(float interval)
			{
				this.interval = interval;
				lastUpdate = -interval;
			}

			public bool CanUpdate()
			{
				if (Time.timeSinceLevelLoad - lastUpdate > interval)
				{
					lastUpdate = Time.timeSinceLevelLoad;
					return true;
				}
				return false;
			}
		}


		private void Awake()
		{
			if (m_Instance == null)
				m_Instance = this;
			else if (m_Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			DwCore.SetLayerInfo(m_LayerInfo);
			m_UpdateScreen = new ScreenHandler(m_ScreenSetting, transform);
			m_UpdateWindow = new WindowHandler(m_WindowSetting, transform);
		}

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
			m_UpdateWindow.Reset();
			m_UpdateScreen.Reset();
		}

		private void Update()
		{
			if (m_ScreenUpdate.CanUpdate())
				m_UpdateScreen.Execute();
			if (m_WindowUpdate.CanUpdate())
				m_UpdateWindow.Execute();
		}

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
			{
				var screenInfo = DwCore.GetScreenInfo();
				foreach (var screen in screenInfo)
				{
					screen.rect.GizmosDraw(m_ScreenSetting.m_ScreenColor);
				}
				return;
			}

			if (m_UpdateScreen != null)
				m_UpdateScreen.OnGizmosDraw();
		}
		
		public IEnumerable<DwScreen> GetScreens()
		{
			if (m_UpdateScreen == null)
				yield break;
			foreach (var screen in m_UpdateScreen.GetScreens())
				yield return screen;
		}
		public bool TryGetScreenById(uint id, out DwScreen screen)
		{
			screen = default;
			if (m_UpdateScreen == null)
				return false;
			return m_UpdateScreen.TryGetById(id, out screen);
		}
		public IEnumerable<DwWindow> GetWindows()
		{
			if (m_UpdateWindow == null)
				yield break;
			foreach (var win in m_UpdateWindow.GetWindows())
				yield return win;
		}
		public bool TryGetWindowById(uint id, out DwWindow window)
		{
			window = null;
			if (m_UpdateWindow == null)
				return false;
			return m_UpdateWindow.TryGetById(id, out window);
		}
	}


	public abstract class OSTask<KEY, DATA> : MyTaskWithState
	{
		public class Cache
		{
			public Dictionary<KEY, DATA> dict;
			public List<KEY> beingAdded, beingRemoved;
			public KEY[] current;

			public Cache(int capacity = 2)
			{
				dict = new Dictionary<KEY, DATA>(capacity);
				beingAdded = new List<KEY>(capacity);
				beingRemoved = new List<KEY>(capacity);
				current = new KEY[0];
			}
			public void Reset()
			{
				dict.Clear();
				beingAdded.Clear();
				beingRemoved.Clear();	
				current= new KEY[0];
			}
		}
		protected Cache m_Cache;
		public OSTask()
		{
			m_Cache = new Cache();
		}

		protected void DetectAddOrRemove(Cache cache, KEY[] toData)
		{
			DetectAddOrRemove(cache.current, toData, ref cache.beingAdded, ref cache.beingRemoved);
			cache.current = toData;
			for (int i = 0; i < m_Cache.beingRemoved.Count; ++i)
			{
				var id = m_Cache.beingRemoved[i];
				if (m_Cache.dict.TryGetValue(id, out var screen))
				{
					OnRemoved(id, screen);
					m_Cache.dict.Remove(id);
				}
			}
			for (int i = 0; i < m_Cache.beingAdded.Count; ++i)
			{
				var id = m_Cache.beingAdded[i];
				if (m_Cache.dict.TryGetValue(id, out DATA record))
				{
					//throw new System.Exception($"Duplicate data {id}");
					Debug.LogError($"Duplicate data {id}");
					continue;
				}
				
				OnAdded(id, record);
				m_Cache.dict.Add(id, record);
			}
		}

		protected void DetectAddOrRemove<T>(IList<T> from, IList<T> to,
			ref List<T> add, ref List<T> rmd)
		{
			Missing(ref add, from, to);
			Missing(ref rmd, to, from);
			void Missing(ref List<T> missing, in IList<T> a, in IList<T> b)
			{
				missing.Clear();
				for (int i = 0; i < b.Count; ++i)
				{
					if (!a.Contains(b[i]))
					{
						if (!missing.Contains(b[i]))
						{ 
							missing.Add(b[i]);
						}
						else
						{
							Debug.Log("Why duplicate?");
						}
					}
				}
			}
		}
		protected abstract void OnAdded(KEY key, DATA data);
		protected abstract void OnRemoved(KEY key, DATA data);

		public virtual void OnGizmosDraw() { }

		public override void Reset()
		{
			base.Reset();
			m_Cache.Reset();
		}
	}

	public class ScreenHandler : OSTask<uint, ScreenInfo>
	{
		[System.Serializable]
		public class Setting
		{
			public Color m_ScreenColor = Color.blue;
		}
		private Setting setting;
		private readonly Transform transform;
		public ScreenHandler(Setting _setting, Transform transform) : base()
		{
			this.setting = _setting;
			this.transform = transform;
		}

		private Dictionary<uint, DwScreen> mapping;
		public IEnumerable<DwScreen> GetScreens()
		{
			if (mapping == null)
				yield break;
			foreach (var screen in mapping.Values)
			{
				yield return screen;
			}
		}

		protected override void OnEnter()
		{
			mapping = new Dictionary<uint, DwScreen>(4);
		}
		protected override bool ContinueOnNextCycle()
		{
			var screenInfo = DwCore.GetScreenInfo();
			if (screenInfo == null || screenInfo.Length == 0)
				return true;
			var toScreenIds = screenInfo.Select(o => o.id).ToArray();
			DetectAddOrRemove(m_Cache, toScreenIds);
			foreach (var info in screenInfo)
			{
				if (!mapping.TryGetValue(info.id, out var screen))
				{
					Debug.LogWarning($"Screen {info.id} not exist.");
					continue;
				}
				if (screen == null)
				{
					Debug.LogWarning($"Screen {info.id}, lost references.");
					continue;
				}
				screen.Init(info);
			}
			foreach (var screen in mapping.Values)
			{
				screen.CullOverlap();
			}
			return true;
		}

		protected override void OnAdded(uint key, ScreenInfo data)
		{
			var screen = new GameObject($"[screen({data.id})]").AddComponent<DwScreen>();
			screen.transform.SetParent(transform, true);
			screen.Init(data);
			mapping.Add(key, screen);
		}

		protected override void OnRemoved(uint key, ScreenInfo data)
		{
			if (!mapping.TryGetValue(key, out var screen))
				throw new System.Exception($"Invalid screen {key}");
			GameObject.Destroy(screen.gameObject);
			mapping.Remove(key);
		}

		protected override void OnComplete() { }

		protected override void OnDisposing()
		{
			base.OnDisposing();
			foreach (var screen in mapping.Values)
			{
				GameObject.Destroy(screen.gameObject);
			}
			mapping.Clear();
		}

		public override void OnGizmosDraw()
		{

			if (mapping == null)
				return;
			foreach (var screen in mapping.Values)
			{
				screen.rect.GizmosDraw(setting.m_ScreenColor);
			}
		}
		public bool TryGetById(uint id, out DwScreen screen)
		{
			screen = default;
			if (mapping == null)
				return false;
			return mapping.TryGetValue(id, out screen);
		}

		public override void Reset()
		{
			base.Reset();
			mapping.Clear();
		}
	}

	public class WindowHandler : OSTask<uint, WindowInfo>
	{
		[System.Serializable]
		public class Setting
		{
			public Color m_WindowColor = Color.green;
		}
		private Setting setting;
		private readonly Transform transform;
		public WindowHandler(Setting _setting, Transform transform) : base()
		{
			this.setting = _setting;
			this.transform = transform;
		}
		private Dictionary<uint, DwWindow> mapping;
		public IEnumerable<DwWindow> GetWindows()
		{
			if (mapping == null)
				yield break;
			foreach (var win in mapping.Values)
			{
				yield return win;
			}
		}
		protected override void OnEnter()
		{
			mapping = new Dictionary<uint, DwWindow>(4);
		}
		protected override bool ContinueOnNextCycle()
		{
			//var windowInfo = DwCore.GetWindowsByOrder();
			var windowInfo = DwCore.GetVisibleNormalWindows(false);
			var toWindowIds = windowInfo.Select(o => o.id).ToArray();
			DetectAddOrRemove(m_Cache, toWindowIds);
			foreach (var info in windowInfo)
			{
				mapping.TryGetValue(info.id, out var win);
				if (win is not DwWindow window)
				{
					Debug.LogError($"Invalid window {info.id}");
					continue;
					//throw new System.Exception($"Invalid window {info.id}");
				}
				window.Init(info);
			}
			return true;
		}
		protected override void OnAdded(uint key, WindowInfo data)
		{
			var window = new GameObject($"[window-NonInit]").AddComponent<DwWindow>();
			window.transform.SetParent(transform, true);
			window.Init(data);
			mapping.Add(key, window);
		}
		protected override void OnRemoved(uint key, WindowInfo data)
		{
			if (!mapping.TryGetValue(key, out var window))
			{
				Debug.LogError($"Invalid window {key}");
				return;
			}
			GameObject.Destroy(window.gameObject);
			mapping.Remove(key);
		}
		protected override void OnComplete() { }
		protected override void OnDisposing()
		{
			base.OnDisposing();
			foreach (var window in mapping.Values)
			{
				GameObject.Destroy(window.gameObject);
			}
			mapping.Clear();
		}
		public override void OnGizmosDraw()
		{
			if (mapping == null)
				return;
			foreach (var window in mapping.Values)
			{
				window.rect.GizmosDraw(setting.m_WindowColor);
			}
		}

		public bool TryGetById(uint id, out DwWindow window)
		{
			window = default;
			if (mapping == null)
				return false;
			return mapping.TryGetValue(id, out window);
		}
		public override void Reset()
		{
			base.Reset();
			mapping.Clear();
		}
	}
}