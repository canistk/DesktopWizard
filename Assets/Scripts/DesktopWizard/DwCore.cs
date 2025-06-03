#define TRY_CATCH
using Kit2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DesktopWizard
{
    public class DwCore
    {
        #region Auto init singleton
        [RuntimeInitializeOnLoadMethod]
        private static void AutoCreateInstance()
        {
            // to trigger static constructor
            instance.Init();
        }

		private static DwCore m_Instance = null;
        public static DwCore instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DwCore();
                return m_Instance;
            }
        }

		private void Init()
        {
            if (state == ePreload.NotStarted)
                state++; // start preload.
        }
		#endregion Auto init singleton

		#region Win32 DLL Error
		private DwCore()
		{
			s_EnumWindowsProc = _EnumWindowsProc;
			GCHandle.Alloc(s_EnumWindowsProc); // freeze delegate to avoid IL2CPP error.

			s_EnumMonitorProc = _EnumMonitorProc;
            GCHandle.Alloc(s_EnumMonitorProc); // freeze delegate to avoid IL2CPP error.

			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}
		~DwCore()
		{
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		}
		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			Debug.LogError($"Unhandled Exception : {args.ExceptionObject}");
		}
		#endregion Win32 DLL Error

		#region Preload
		public enum ePreload
        {
            NotStarted = 0,
            InitJava,
            // Jint init
            // read plugin package
            // init plugin one by one
            PreloadComplete,
        }
        public delegate void StateChanged(ePreload from, ePreload to);
        public static event StateChanged EVENT_StateChanged;
        private ePreload m_State = ePreload.NotStarted;
        public ePreload state
        {
            get => m_State;
            private set
            {
                //is this returning early? EVENT_StateChanged don't seem to fire
                if (m_State == value)
                    return;
                var old = m_State;
                m_State = value;
                EnterState(m_State);
                EVENT_StateChanged?.Invoke(old, m_State);
            }
        }
        void _NextState() => state++;
        private void EnterState(ePreload next)
        {
            switch (next)
            {
                default: throw new System.NotImplementedException($"State = {next}");
                case ePreload.NotStarted: break;
                case ePreload.InitJava: InitJava(); break;
                case ePreload.PreloadComplete: PreloadComplete(); break;
            }

            void InitJava()
            {
                _NextState();
            }
        }

        private void PreloadComplete()
        {
            m_IsInitialized = true;
            foreach (var c in m_TaskOnPreload)
            {
                c?.TryCatchDispatchEventError(o => o?.Invoke());
            }
            m_TaskOnPreload.Clear();
        }
        #endregion Preload

        #region Wait for preload
        private bool m_IsInitialized = false;
        private List<System.Action> m_TaskOnPreload = new List<System.Action>(8);
        public void WaitForPreloadCompleted(System.Action callback)
        {
            if (callback == null)
            {
                Debug.LogError("Invalid callback try to wait for preload complete.");
                return;
            }

            if (m_IsInitialized)
            {
                // already init, just execute the callback.
                callback?.Invoke();
                return;
            }

            m_TaskOnPreload.Add(callback);
        }
        #endregion Wait for preload

        #region Fetch Desktop Window(s)
        
        [DllImport("user32.dll")] // ok
		private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

		/// Note : add [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))] to avoid IL2CPP error.
		//[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
		private static EnumWindowsProc s_EnumWindowsProc = _EnumWindowsProc;


		[DllImport("user32.dll")] // ok
		private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

		/// Note : add [AOT.MonoPInvokeCallback(typeof(MonitorEnumProc))] to avoid IL2CPP error.
		// [UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private delegate bool MonitorEnumProc(IntPtr hDesktop, IntPtr hdc, ref OSRect pRect, int dwData);
        private static MonitorEnumProc s_EnumMonitorProc = _EnumMonitorProc;


		[DllImport("user32.dll")] // ok
        private static extern IntPtr GetTopWindow(IntPtr hWnd = default);
        
        [DllImport("user32.dll")] // ok
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", SetLastError = true)] // ok
		private static extern bool GetWindowRect(IntPtr hWnd, out OSRect lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetClientRect(IntPtr hWnd, out OSRect lpRect);

		[DllImport("user32.dll", SetLastError = true)] // ok
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")] // ok
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")] // ok
        private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32.dll")] // ok
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")] // ok
        private static extern int GetWindowLongPtrW(IntPtr hWnd, int nIndex);

		/// <summary>
		/// <see cref="https://learn.microsoft.com/zh-tw/windows/win32/winmsg/extended-window-styles"/>
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns></returns>
		public static WinExStyle GetWindowExStyle(IntPtr hWnd)
        {
#if TRY_CATCH
			try
#endif
			{
                const int GWL_EXSTYLE = -20;
                if (hWnd ==  IntPtr.Zero) throw new System.Exception("hWnd is null");
                return (WinExStyle)GetWindowLongPtrW(hWnd, GWL_EXSTYLE); // try..catch
            }
#if TRY_CATCH
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return (WinExStyle)0;
            }
#endif
		}

		public static WinStyle GetWindowStyle(IntPtr hWnd)
        {
#if TRY_CATCH
			try
#endif
			{
                const int GWL_STYLE = -16;
				if (hWnd == IntPtr.Zero) throw new System.Exception("hWnd is null");
				return (WinStyle)GetWindowLongPtrW(hWnd, GWL_STYLE); // try..catch
			}
#if TRY_CATCH
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
				return (WinStyle)0;
            }
#endif
        }

        private static List<IntPtr> s_hWndList = new List<IntPtr>(128);
        private static StringBuilder sb = new StringBuilder(128);

		/// <summary>Get windows information and return by their z-order
		/// optimized : result will be cached in the same frame.</summary>
		public static WindowInfo[] GetWindowsByOrder()
        {
            if (s_WindowInfos.Key == Time.frameCount)
                return s_WindowInfos.Value;
            
            lock (s_hWndList)
            {
			    s_hWndList.Clear();
            }
#if TRY_CATCH
			try
#endif
			{
                if (!EnumWindows(s_EnumWindowsProc, IntPtr.Zero)) // try..catch
                {
                    /// <see cref="_EnumWindowsProc(IntPtr, IntPtr)"/>
                    var error = $"{Marshal.GetLastWin32Error()}";
					Debug.LogError($"EnumWindows failed, Error: {error}");
					return new WindowInfo[0];
				}
				/// IL2CPP does not support marshaling delegates that point to instance methods to native code.
            }
#if TRY_CATCH
			catch (System.Exception ex)
            {
				Debug.LogError($"loop via window EnumWindows fail.\n{ex.Message}\n{ex.StackTrace}");
				return new WindowInfo[0];
			}
#endif
			if (s_hWndList.Count == 0)
			{
				Debug.LogWarning("No window found.");
				return new WindowInfo[0];
			}


			var dict = new Dictionary<IntPtr, WindowInfo>(s_hWndList.Count);
#if TRY_CATCH
			try
#endif
			{
                lock (s_hWndList)
                {
                    for (var i = 0; i < s_hWndList.Count; ++i)
                    {
                        var hWnd = s_hWndList[i];
                        if (hWnd == IntPtr.Zero)
                        {
                            Debug.LogWarning("s_hWndList shouldn't has null value.");
                            continue;
                        }
                        if (!IsWindow(hWnd)) // try..catch
                            continue;
                        if (!IsWindowVisible(hWnd)) // try..catch
                            continue;
                        int length = GetWindowTextLength(hWnd); // try..catch
                        if (length == 0)
                            continue;
                        if (!GetWindowRect(hWnd, out OSRect rect)) // try..catch
                        {
                            Debug.LogError($"GetWindowRect failed for hWnd 0x{hWnd.ToInt64():X8}, Error: {Marshal.GetLastWin32Error()}");
                            continue;
                        }
                        sb.Clear();
                        _ = GetWindowText(hWnd, sb, length + 1); // try..catch
                        var win = new WindowInfo(hWnd, sb.ToString(), rect, 0u);
                        dict.Add(hWnd, win);
                    }
                }
            }
#if TRY_CATCH
			catch (System.Exception ex)
            {
				Debug.LogError($"Define window order fail.\n{ex.Message}\n{ex.StackTrace}");
				return new WindowInfo[0];
            }
#endif

            const uint GW_HWNDNEXT = 2;
			var windowInfos = new List<WindowInfo>(128);
#if TRY_CATCH
			try
#endif
			{
			    var order       = 0u;
                var hWnd        = GetTopWindow(); // try..catch
				if (hWnd == IntPtr.Zero)
                    throw new System.Exception("GetTopWindow() fail, hWnd is null");
				do
                {
					if (dict.TryGetValue(hWnd, out var win))
                    {
                        win.order = ++order;
                        windowInfos.Add(win);
                    }
					hWnd = GetWindow(hWnd, GW_HWNDNEXT); // try..catch, Get Next hWnd
				}
                while (hWnd != IntPtr.Zero && order < dict.Count);
            }
#if TRY_CATCH
			catch (System.Exception ex)
            {
				Debug.LogError($"Fetching and remap window order fail.\n{ex.Message}\n{ex.StackTrace}");
                if (windowInfos == null)
					return new WindowInfo[0];
			}
#endif
            var arr = windowInfos.ToArray();
            s_WindowInfos = new KeyValuePair<int, WindowInfo[]>(Time.frameCount, arr);
            return s_WindowInfos.Value;

        }

		[AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
		private static bool _EnumWindowsProc(IntPtr hWnd, IntPtr lParam)
		{
            if (hWnd == IntPtr.Zero)
				return false;
			s_hWndList.Add(hWnd);
			return true;
		}

		private static KeyValuePair<int /* frame count */, WindowInfo[]> s_WindowInfos = new KeyValuePair<int, WindowInfo[]>(-1, new WindowInfo[0]);
        public static bool TryGetWindowById(uint id, out WindowInfo windowInfo)
        {
            var wins = GetWindowsByOrder();
            for (int i = 0; i < wins.Length; ++i)
            {
                if (wins[i].id != id)
                    continue;
                windowInfo = wins[i];
                return true;
            }
            windowInfo = default;
            return false;
        }

        public static bool TryGetActiveWindow(out WindowInfo windowInfo)
        {
			windowInfo = default;
			if (!_TryGetForegroundWindow(out var id))
				return false;
			return TryGetWindowById(id, out windowInfo);
		}

		/// <summary>Get current application's active window.</summary>
		/// <param name="windowInfo"></param>
		/// <returns></returns>
		public static bool TryGetCurrentApplicationWindow(out WindowInfo windowInfo)
		{
            uint id = (uint)_GetActiveWindow(); // try..catch
            if (id == 0)
			{
				windowInfo = default;
				return false;
			}
			return TryGetWindowById(id, out windowInfo);
		}

        /// <summary>All visible windows, *only* exclude those behind Fullscreen.</summary>
        /// <returns></returns>
		public static IEnumerable<WindowInfo> GetVisibleNormalWindows(bool includeFullscreen)
		{
			var screens = GetScreenInfo();
            var screenDict = new Dictionary<uint, uint>();

			foreach (var win in GetBiasWindows())
			{
                var foundScr = false;
                for (int i = 0; i < screens.Length && !foundScr; ++i)
                {
					if (!win.rect.Overlaps(screens[i].rect))
                        continue;

					// Since fullscreen in window will larger than the screen.rect,
					// therefore we need to filter out the one which just overlap an edge. bias 90%
					if (win.IsMaximize)
					{
						var percent = win.rect.OverlapPercentage(screens[i].rect);
						if (percent < 0.9f)
							continue;
					}

					foundScr = true;
                    // Case 1 : it's First maximize window on screen.(show)
                    // Case 2 : it's second maximize window on screen. (hide)
                    // Case 3 : it's normal window before fullscreen (show)
                    // Case 4 : it's normal window after fullscreen (hide)
                    var screenId = screens[i].id;
                    var hadFullScreenAlready = screenDict.TryGetValue(screenId, out var fullScreenIdx);
                    var firstFullscreenWin = win.IsMaximize && !hadFullScreenAlready;
                    var windowAfterFullscreen = hadFullScreenAlready && win.order > fullScreenIdx;

                    if (firstFullscreenWin)
                    {
                        screenDict[screenId] = win.order;
                    }

                    // Block logic
                    if (!includeFullscreen && win.IsMaximize)
                        continue;
                    if (windowAfterFullscreen)
                        continue;
                    yield return win;
				}
			}
		}

        /// <summary>All fullscreen windows. across all screens.</summary>
        /// <returns></returns>
        public static IEnumerable<WindowInfo> GetFullscreenWindows()
		{
			var screens = GetScreenInfo();
			var wins = GetBiasWindows().ToArray();
			for (var i = 0; i < screens.Length; ++i)
			{
				for (var k = 0; k < wins.Length; ++k)
				{
					var win = wins[k];
					if (!screens[i].rect.Overlaps(win.rect))
						continue;
					if (win.IsMaximize)
					{
						yield return win;
					}
				}
			}
		}

        /// <summary>Get all Screen(s) that overlap with the given window.
        /// fail when window not exist, or out of screen area.</summary>
        public static IEnumerable<ScreenInfo> GetWindowDisplayInScreens(uint id)
        {
            if (!TryGetWindowById(id, out var win))
                yield break;
            var screens = GetScreenInfo();
            for (int i = 0; i < screens.Length; ++i)
			{
				if (screens[i].rect.Overlaps(win.rect))
					yield return screens[i];
			}
        }

		/// <summary>Get Screen information and return
		/// optimized : result will be cached in the same frame</summary>
		/// <returns></returns>
		public static ScreenInfo[] GetScreenInfo()
        {
            if (s_ScreenInfos.Key != Time.frameCount)
            {
#if TRY_CATCH
				try
#endif
				{
                    lock (s_MonitorList)
                    {
                        s_MonitorList.Clear();
                        if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, s_EnumMonitorProc, 0)) // try..catch
                        {
                            var error = $"{Marshal.GetLastWin32Error()}";
                            Debug.LogError($"GetScreenInfo failed, Error: {error}\ns_MonitorList={s_MonitorList.Count}");
                            return new ScreenInfo[0];
                        }
                        s_ScreenInfos = new KeyValuePair<int, ScreenInfo[]>(Time.frameCount, s_MonitorList.ToArray());
                    }
                }
#if TRY_CATCH
				catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    return new ScreenInfo[0];
                }
#endif
            }
            return s_ScreenInfos.Value;
        }

        private static List<ScreenInfo> s_MonitorList = new List<ScreenInfo>(4);
		// IL2CPP does not support marshaling delegates that point to instance methods to native code.
		[AOT.MonoPInvokeCallback(typeof(MonitorEnumProc))]
		private static bool _EnumMonitorProc(IntPtr hDesktop, IntPtr hdc, ref OSRect rect, int d)
		{
            lock (s_MonitorList)
            {
                s_MonitorList.Add(new ScreenInfo(hDesktop, rect));
                return s_MonitorList.Count > 0;
            }
		}

		private static KeyValuePair<int, ScreenInfo[]> s_ScreenInfos = new KeyValuePair<int, ScreenInfo[]>(-1, new ScreenInfo[0]);
        public static bool TryGetScreenById(int id, out ScreenInfo screenInfo)
        {
            var scrs = GetScreenInfo();
            for (int i = 0; i< scrs.Length; ++i)
			{
				if (scrs[i].id != id)
					continue;
				screenInfo = scrs[i];
				return true;
			}
            screenInfo = default;
            return false;
        }
		#endregion Fetch Desktop Window(s)

		#region Taskbar
		[DllImport("shell32.dll", SetLastError = true)] // ok
		private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

		[StructLayout(LayoutKind.Sequential)]
		private struct APPBARDATA
		{
			public uint cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public uint uEdge;
			public OSRect rect;
			public int lParam;
		}

        public struct AppBarInfo
        {
			public uint id;
            public IntPtr hWnd => (IntPtr)id;
			public ABE edge;
			public OSRect rect;
			public int lParam;
		}

		public enum ABE : uint
		{
			UNKNOWN = 0xFFFFFFFF,
			LEFT = 0,
			TOP = 1,
			RIGHT = 2,
			BOTTOM = 3,
		}

		public static IEnumerable<AppBarInfo> GetAllTaskbars()
		{
			const uint ABM_GETTASKBARPOS = 0x00000005;
			APPBARDATA t = new APPBARDATA();
            HashSet<OSRect> rectSet = new HashSet<OSRect>();
            foreach (var w in GetWindowsByOrder())
			{
				t.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
				t.hWnd = w.hWnd;
                if (t.hWnd == IntPtr.Zero)
                    continue;
				IntPtr result = IntPtr.Zero;

				try
                {
                    result = SHAppBarMessage(ABM_GETTASKBARPOS, ref t); // try..catch
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    continue;
                }

                if (result == IntPtr.Zero)
					continue;

                if (rectSet.Contains(t.rect))
                    continue;
                rectSet.Add(t.rect);

				var taskbar = new AppBarInfo
				{
					id = (uint)t.hWnd,
					edge = (ABE)t.uEdge,
					rect = t.rect,
					lParam = t.lParam,
				};
				yield return taskbar;
			}
		}

		public static bool TryGetTaskbarPosition(out AppBarInfo bar)
		{
			const uint ABM_GETTASKBARPOS = 0x00000005;
			APPBARDATA data = default;
			data.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
            IntPtr result = IntPtr.Zero;
            bar = default;
#if TRY_CATCH
			try
#endif
			{
				result = SHAppBarMessage(ABM_GETTASKBARPOS, ref data); // try..catch
            }
#if TRY_CATCH
			catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
#endif

			if (result == IntPtr.Zero)
			{
				return false;
			}
            if (data.hWnd == IntPtr.Zero)
            {
                return false;
            }
			bar = new AppBarInfo
			{
				id = (uint)data.hWnd,
				edge = (ABE)data.uEdge,
				rect = data.rect,
				lParam = data.lParam,
			};
			return bar.edge != ABE.UNKNOWN;
		}
		#endregion Taskbar

		#region Mouse
#if false
		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);

        /// <summary>Get cursor position from OS directly
        /// this work even U3D not getting focus.</summary>
        /// <returns>The position in OS space</returns>
        public static Vector2Int GetOSCursorPos()
		{
            // C++ method callback, slower than System.Windows.Forms.Cursor.Position
			GetCursorPos(out POINT p);
			return new Vector2Int(p.x, p.y);
		}
#else
		/// <summary>Get cursor position from OS directly
		/// this work even U3D not getting focus.</summary>
		/// <returns>The position in OS space</returns>
		public static Vector2Int GetOSCursorPos()
		{
#if TRY_CATCH
			try
#endif
			{
                var _c = System.Windows.Forms.Cursor.Position;
                return new Vector2Int(_c.X, _c.Y);
            }
#if TRY_CATCH
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
				return new Vector2Int(0, 0);
			}
#endif
		}
#endif

		#endregion Mouse

		#region Internal API
		private static IEnumerable<WindowInfo> GetBiasWindows()
        {
            var ignoreStyle     = WinStyle.MINIMIZE;
			//var ignoreExStyle   = WinExStyle.LAYERED | WinExStyle.NOREDIRECTIONBITMAP | WinExStyle.TOOLWINDOW;
			var ignoreExStyle = WinExStyle.NOREDIRECTIONBITMAP | WinExStyle.TOOLWINDOW;
			foreach (var w in GetWindowsByOrder())
            {
                if ((w.style & ignoreStyle) != 0)
                    continue;
                if ((w.exStyle & ignoreExStyle) != 0)
                    continue;
                yield return w;
            }
        }
        #endregion Internal API

        #region BMP repaint
		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref POINT psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNC pblend, int dwFlags);
        internal static int _UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref POINT psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNC pblend, int dwFlags)
        {
#if TRY_CATCH
			try
			{
#endif
                return UpdateLayeredWindow(hwnd, hdcDst, ref pptDst, ref psize, hdcSrc, ref pprSrc, crKey, ref pblend, dwFlags);

#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
                return 0;
			}
#endif
		}

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);
        internal static IntPtr _GetDC(IntPtr hWnd)
        {
#if TRY_CATCH
			try
			{
#endif
                return GetDC(hWnd);
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return IntPtr.Zero;
			}
#endif
		}

		[DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        internal static int _ReleaseDC(IntPtr hWnd, IntPtr hDC)
        {
#if TRY_CATCH
			try
			{
#endif
				if (hDC == IntPtr.Zero)
					return 0;
				return ReleaseDC(hWnd, hDC);
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return 0;
			}
#endif
		}

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        internal static IntPtr _CreateCompatibleDC(IntPtr hDC)
        {
#if TRY_CATCH
			try
			{
#endif
				if (hDC == IntPtr.Zero)
					return IntPtr.Zero;
				return CreateCompatibleDC(hDC);
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return IntPtr.Zero;
			}
#endif
		}

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int DeleteDC(IntPtr hdc);
        internal static int _DeleteDC(IntPtr hdc)
        {
#if TRY_CATCH
			try
			{
#endif
				if (hdc == IntPtr.Zero)
					return 0;
				return DeleteDC(hdc);
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return 0;
			}
#endif
		}

		[DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        internal static IntPtr _SelectObject(IntPtr hDC, IntPtr hObject)
        {
#if TRY_CATCH
			try
			{
#endif
                return SelectObject(hDC, hObject);
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return IntPtr.Zero;
			}
#endif
		}

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int DeleteObject(IntPtr hObject);
        internal static int _DeleteObject(IntPtr hObject)
        {
#if TRY_CATCH
			try
			{
#endif
                return DeleteObject(hObject);
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return 0;
			}
#endif
		}
		#endregion BMP repaint

		#region Window Form
		[DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern short UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll")] // ok
        private static extern IntPtr GetActiveWindow();
        internal static IntPtr _GetActiveWindow()
        {
#if TRY_CATCH
            try
            {
#endif
                return GetActiveWindow();
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return IntPtr.Zero;
			}
#endif
		}

		[DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
		internal static bool _SetForegroundWindow(IntPtr hWnd)
		{
#if TRY_CATCH
			try
			{
#endif
				return SetForegroundWindow(hWnd);
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return false;
			}
#endif
		}

		[DllImport("user32.dll")] // ok
		private static extern IntPtr GetForegroundWindow();
		internal static bool _TryGetForegroundWindow(out uint id)
		{
            id = 0u;
#if TRY_CATCH
			try
			{
#endif
				IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero)
                    return false;
                id = (uint)hWnd;
                return true;
#if TRY_CATCH
			}
			catch (System.Exception ex)
			{
				var win32Err = $"Win32Error:{Marshal.GetLastWin32Error()}";
				Debug.LogError($"{win32Err}\n{ex.Message}\n{ex.StackTrace}");
				return false;
			}
#endif
		}
		#endregion Window Form

		#region OS
		public bool TryGetOS(out DwOSBody os)
        {
            os = DwOSBody.instance;
            return os != null;
		}
		#endregion OS

		#region Share Layer
		[System.Serializable]
		public struct LayerInfo
        {
            [LayerField] public string m_ScreenLayer;
			[LayerField] public string m_RawBorderLayer;
			[LayerField] public string m_BorderLayer;
            [LayerField] public string m_WindowLayer;
		}
        public struct LayerInfoFetched
		{
			public int ScreenLayer;
			public int ScreenLayerMask;
            public int RawBorderLayer;
			public int RawBorderLayerMask;
			public int BorderLayer;
			public int BorderLayerMask;
            public int WindowLayer;
			public int WindowLayerMask;
		}
		private static LayerInfoFetched m_LayerInfo = default;
        public static LayerInfoFetched layer => m_LayerInfo;
        public static void SetLayerInfo(LayerInfo info)
		{
            m_LayerInfo = default;
			if (!string.IsNullOrEmpty(info.m_ScreenLayer))
			{
				m_LayerInfo.ScreenLayer = LayerMask.NameToLayer(info.m_ScreenLayer);
				m_LayerInfo.ScreenLayerMask = LayerMask.GetMask(info.m_ScreenLayer);
			}
            if (!string.IsNullOrEmpty(info.m_RawBorderLayer))
            {
                m_LayerInfo.RawBorderLayer = LayerMask.NameToLayer(info.m_RawBorderLayer);
                m_LayerInfo.RawBorderLayerMask = LayerMask.GetMask(info.m_RawBorderLayer);
            }
            if (!string.IsNullOrEmpty(info.m_BorderLayer))
			{
				m_LayerInfo.BorderLayer = LayerMask.NameToLayer(info.m_BorderLayer);
				m_LayerInfo.BorderLayerMask = LayerMask.GetMask(info.m_BorderLayer);
			}
			if (!string.IsNullOrEmpty(info.m_WindowLayer))
			{
				m_LayerInfo.WindowLayer = LayerMask.NameToLayer(info.m_WindowLayer);
				m_LayerInfo.WindowLayerMask = LayerMask.GetMask(info.m_WindowLayer);
			}
		}
		#endregion Share Layer
	}

	#region Win2U3D structure
	[System.Serializable]
	public struct WindowInfo
    {
        public readonly uint id;
        public IntPtr hWnd => (IntPtr)id;
        public readonly string title;
        public readonly WinExStyle exStyle;
        public readonly WinStyle style;
        public readonly OSRect rect;

        [Tooltip("smaller at the top (active=1), larger in behind.")]
		public /*readonly*/ uint order;
        public bool isValid { get; private set; }
        public WindowInfo(IntPtr id, string title, OSRect rect, uint order)
        {
			if (id == IntPtr.Zero) throw new System.Exception("hWnd is null");
			this.id         = (uint)id;
            this.title      = title;
            this.rect       = rect;
            this.style      = DwCore.GetWindowStyle(id);
            this.exStyle    = DwCore.GetWindowExStyle(id);
            this.order      = order;
            this.isValid = true;
		}

        public bool IsMaximize => (style & WinStyle.MAXIMIZE) != 0;
        public bool IsMinimize => (style & WinStyle.MINIMIZE) != 0;

		public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool detail)
        {
            if (detail)
                return $"[{id}]{title}\norder={order}\n{rect}\nStyle={style}\nExStyle={exStyle}";
            else
                return $"[{id}]{title}\norder={order}";
        }

        public bool Overlaps(OSRect other) => this.rect.Overlaps(other);

        public void MoveTo(int x, int y) => MoveTo(new Vector2Int(x, y));
        public void MoveTo(Vector2Int pos)
        {
            const uint SWP_NOACTIVATE = 0x0010;
			const uint SWP_NOZORDER = 0x0004;
            const uint SWP_NOSIZE   = 0x0001;
            try
            {
                // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
                DwCore.SetWindowPos(hWnd, IntPtr.Zero, pos.x, pos.y, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE); // try..catch
                // UnityEngine.Debug.Log($"[{hWnd}] MoveTo({pos})");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void MoveBy(int x, int y) => MoveBy(new Vector2Int(x, y));
        public void MoveBy(Vector2Int pos)
        {
            pos.x += this.rect.Left;
            pos.y += this.rect.Top;
            MoveTo(pos);
        }

    }
	
    [System.Serializable]
	public struct ScreenInfo
    {
        public readonly uint id;
        public readonly OSRect rect;
        public ScreenInfo(IntPtr id, OSRect rect)
        {
            this.id = (uint)id;
            this.rect = rect;
        }
        public override string ToString()
        {
            return $"Screen[{id}] resolution={rect}";
        }

        public bool Overlaps(WindowInfo other) => this.rect.Overlaps(other.rect);
    }

    public struct MonitorRaycastResult
    {
        public Vector2 orgin;
        public Vector2 direction;
        public float distance;

        /// <summary>EndPoint or HitPoint based on collider hit or ray end position</summary>
        public Vector3 endPoint;
        
        public Matrix4x4 m2f, f2m;
        public Matrix4x4 o2m, m2o;
        public Matrix4x4 o2f, f2o;

        public Collider collider;
        public bool isBorder, isWindow;

        public DwWindow window;

        public void Fetch(DwCamera dwCamera)
        {
            m2f = dwCamera.MatrixMonitorToForm();
            f2m = m2f.inverse;
            o2m = dwCamera.MatrixOSToMonitor();
            m2o = o2m.inverse;
            o2f = dwCamera.MatrixOSToForm();
            f2o = o2f.inverse;
        }

        public void CalcOS(out Vector2Int _pos, out Vector2 _dir, out float _distance, out Vector3 _hitPoint)
        {
            var p = m2o.MultiplyPoint3x4(orgin);
            _pos = new Vector2Int((int)p.x, (int)p.y);

            var vector = m2o.MultiplyVector(direction.normalized * distance);
            _dir = vector.normalized;
            _distance = vector.magnitude;
			_hitPoint = m2o.MultiplyPoint3x4(this.endPoint);
		}

        public void CalcForm(out Vector2 _pos, out Vector2 _dir, out float _distance, out Vector3 _hitPoint)
        {
            var p = m2f.MultiplyPoint3x4(orgin);
            _pos = new Vector2(p.x, p.y);

            var vector = m2f.MultiplyVector(direction.normalized * distance);
            _dir = vector.normalized;
            _distance = vector.magnitude;
            _hitPoint = m2f.MultiplyPoint3x4(this.endPoint);
        }

        public void CalcMonitor(out Vector2 _pos, out Vector2 _dir, out float _distance, out Vector3 _hitPoint)
        {
            _pos = this.orgin;
            _dir = this.direction;
            _distance = this.distance;
            _hitPoint = this.endPoint;
        }
    }
    #endregion Win2U3D structure

    #region Window Data Structure
    [System.Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OSRect : IEquatable<OSRect>
    {
        public int Left, Top, Right, Bottom;
        public override string ToString()
        {
            return $"[Rect L:{Left},T:{Top},R:{Right},B:{Bottom}]";
        }

        public Vector2[] GetCorners()
        {
            return new Vector2[4]
            {
                new Vector2(Left,   -Bottom),
                new Vector2(Left,   -Top),
                new Vector2(Right,  -Top),
                new Vector2(Right,  -Bottom),
            };
        }

        public bool Contains(Vector2Int p)
        {
            return p.x > Left && p.x < Right && p.y > Top && p.y < Bottom;
        }

        public bool Overlaps(OSRect other)
        {
            return
                !(this.Left >= other.Right || this.Right <= other.Left ||
                this.Top >= other.Bottom || this.Bottom <= other.Top);
        }

		public float OverlapPercentage(OSRect other)
		{
			int overlapLeft = Math.Max(this.Left, other.Left);
			int overlapRight = Math.Min(this.Right, other.Right);
			int overlapTop = Math.Max(this.Top, other.Top);
			int overlapBottom = Math.Min(this.Bottom, other.Bottom);

			if (overlapLeft >= overlapRight || overlapTop >= overlapBottom)
				return 0f;

			int overlapArea = (overlapRight - overlapLeft) * (overlapBottom - overlapTop);
			int rectArea = (this.Right - this.Left) * (this.Bottom - this.Top);

			return ((float)overlapArea / rectArea);
		}

		public bool Equals(OSRect other)
		{
			return
                Left == other.Left &&
                Top == other.Top &&
                Right == other.Right &&
                Bottom == other.Bottom;
		}

		public void DebugDraw(Color? color) => DebugDraw(this, color);
        public static void DebugDraw(OSRect rect, Color? color)
        {
            var c = color.HasValue ? color.Value : Color.white;
            var arr = rect.GetCorners();
            for (int i = 0; i < arr.Length; ++i)
            {
                var p0 = (Vector3)arr[i];
                var p1 = (Vector3)arr[(i + 1) % arr.Length];
                Debug.DrawLine(p0, p1, c, 5f);
            }
        }
        public void GizmosDraw(Color? color) => GizmosDraw(this, color);
        public static void GizmosDraw(OSRect rect, Color? color)
        {
            var c = color.HasValue ? color.Value : Color.white;
            var arr = rect.GetCorners();
            using (new DwColorScope(c))
            {
                for (int i = 0; i < arr.Length; ++i)
                {
                    var p0 = (Vector3)arr[i];
                    var p1 = (Vector3)arr[(i + 1) % arr.Length];
                    Gizmos.DrawLine(p0, p1);
                }
            }
        }
	}

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
        public POINT(int x, int y) { this.x = x; this.y = y; }
        public override string ToString()
        {
            return $"({x},{y})";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BGRA
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;
        public override string ToString()
        {
            return $"(R:{R},G:{G},B:{B},A:{A})";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNC
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    
    [System.Flags]
    public enum WinExStyle : uint
	{
        ACCEPTFILES     = 0x00000010,
        APPWINDOW       = 0x00040000,
        CLIENTEDGE      = 0x00000200,
        COMPOSITED      = 0x02000000,
        CONTEXTHELP     = 0x00000400,
        CONTROLPARENT   = 0x00010000,
        DLGMODALFRAME   = 0x00000001,
        LAYERED         = 0x00080000,
        LAYOUTRTL       = 0x00400000,
        LEFT            = 0x00000000,
        LEFTSCROLLBAR   = 0x00004000,
        LTRREADING      = 0x00000000,
        MDICHILD        = 0x00000040,
        NOACTIVATE      = 0x08000000,
        NOINHERITLAYOUT = 0x00100000,
        NOPARENTNOTIFY  = 0x00000004,
        NOREDIRECTIONBITMAP = 0x00200000,
        RIGHTSCROLLBAR  = 0x00000000,
        STATICEDGE      = 0x00020000,
        TOOLWINDOW      = 0x00000080,
        TOPMOST         = 0x00000008,
        TRANSPARENT     = 0x00000020,
        WINDOWEDGE      = 0x00000100,
    }

    /// <summary>
    /// GWL_STYLE = -16
    /// <see cref="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowlongptrw"/>
    /// <see cref="https://learn.microsoft.com/zh-tw/windows/win32/api/winuser/nf-winuser-showwindow"/>
    /// </summary>
    [System.Flags]
    public enum WinStyle : uint
    {
        BORDER          = 0x00800000,
        CAPTION         = 0x00C00000,
        CHILD           = 0x40000000,
        CHILDWINDOW     = 0x40000000,
        CLIPCHILDREN    = 0x02000000,
        CLIPSIBLINGS    = 0x04000000,
        DISABLED        = 0x08000000,
        DLGFRAME        = 0x00400000,
        GROUP           = 0x00020000,
        HSCROLL         = 0x00100000,
        ICONIC          = 0x20000000,
        MAXIMIZE        = 0x01000000,
        MAXIMIZEBOX     = 0x00010000,
        MINIMIZE        = 0x20000000,
        MINIMIZEBOX     = 0x00020000,
        OVERLAPPED      = 0x00000000,
        SYSMENU         = 0x00080000,
        THICKFRAME      = 0x00040000,
        TILED           = 0x00000000,
        VISIBLE         = 0x10000000,
        VSCROLL         = 0x00200000,
        SIZEBOX         = 0x00040000,
        TABSTOP         = 0x00010000,
        POPUP           = 0x80000000,
        TILEDWINDOW     = OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX,
        OVERLAPPEDWINDOW= OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX,
        POPUPWINDOW     = POPUP | BORDER | SYSMENU
    }

    #endregion Window Data Structure
}