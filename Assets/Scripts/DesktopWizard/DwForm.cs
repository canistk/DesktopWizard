using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine.EventSystems;
using UnityEngine;

namespace DesktopWizard
{
    [Serializable]
    public class FormSetting
    {
        public string id = "Widget";
        public bool TopMost = true;
        [Min(0)] public Vector2Int StartPos = new Vector2Int(300, 300);
        [Min(1)] public Vector2Int Size = new Vector2Int(480, 640);

		[Tooltip("Define which mouse drag can move this window.")]
		public eDragMethod dragMethod = eDragMethod.HoldMouseRightBtn;

        [Tooltip("Capture mouse move event, even Window.Form was lose focus.")]
        public bool CaptureMouseMoveEventOnLoseFocus = false;
	}

    public enum eDragMethod
    {
        None = 0,
        HoldMouseMiddleBtn = 1,
        HoldMouseRightBtn = 2,
	}

	/// <summary>
    /// A wrapper to handle <see cref="System.Windows.Forms.Form"/>,
    /// based on <see cref="DwCamera"/>'s vision and setting.
    /// </summary>
	public class DwForm : Form
    {
        #region Const
        //private const int ULW_COLORKEY = 0x00000001;
        private const int ULW_ALPHA = 0x00000002;
        //private const int ULW_OPAQUE = 0x00000004;

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
		#endregion Const

		private static List<DwForm> s_Forms = new List<DwForm>(8);
        public static IEnumerable<DwForm> GetActiveForms()
        {
			foreach (var f in s_Forms)
                yield return f;
        }

		/// <summary>
		/// The text associated with this control.
        /// As window title.
		/// </summary>
		public sealed override string Text
		{
			get { return base.Text; }
			set { base.Text = value; }
		}

		public DwCamera dwCamera { get; private set; }
        public uint id => hWnd;
        public readonly uint hWnd;
		public DwForm(DwCamera dwCamera) : base()
        {
            this.dwCamera = dwCamera;
			this.hWnd = (uint)Handle;
            var setting = this.dwCamera.setting;

			TopMost = setting.TopMost; //false; // Win bug: on top at first frame will glitch.
            //update topMost on next update @setting.TopMost;

            Text    = string.IsNullOrEmpty(setting.id) ? "Widget" : setting.id;
            SetBounds(setting.StartPos.x, setting.StartPos.y, setting.Size.x, setting.Size.y);

            AllowDrop       = false;
            FormBorderStyle = FormBorderStyle.None;
            ControlBox      = false;
            ShowInTaskbar   = false;
            MaximizeBox     = false;
            MinimizeBox     = false;
            DoubleBuffered  = true; // secondary buffer prevent flicker.
            Capture         = true; // capture mouse/key event(s)

            InitVisionSetting();

            Load += Form_Loaded;
			FormClosed += Form_FormClosed;

            MouseDown += Form_MouseDown;
            MouseUp += Form_MouseUp;
            MouseClick += Form_MouseClick;
            MouseMove += Form_MouseMove;
            MouseWheel += Form_MouseWheel;

            KeyDown += Form_KeyDown;
            KeyUp += Form_KeyUp;

            GotFocus += Form_GotFocus;
            LostFocus += Form_LoseFocus;
			Move += From_Move;
		}
        private void Form_Loaded(object sender, EventArgs e)
		{
			Load -= Form_Loaded;
            s_Forms.Add(this);
		}
		private void Form_FormClosed(object sender, EventArgs e)
        {
            s_Forms.Remove(this);
			Load -= Form_Loaded;
			FormClosed -= Form_FormClosed;

            MouseDown -= Form_MouseDown;
            MouseUp -= Form_MouseUp;
            MouseClick -= Form_MouseClick;
            MouseMove -= Form_MouseMove;
            MouseWheel -= Form_MouseWheel;

            KeyDown -= Form_KeyDown;
            KeyUp -= Form_KeyUp;

            GotFocus -= Form_GotFocus;
            LostFocus -= Form_LoseFocus;
			Move -= From_Move;
		}
		
        /// <summary>Move toward OS space position.</summary>
		/// <param name="v"></param>
		public void MoveTo_OS(Vector2Int v)
        {
            MoveTo_OS(v.x, v.y);
        }
		/// <summary>Move toward OS space position.</summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void MoveTo_OS(int x, int y)
        {
            Left = x;
            Top = y;
        }

        #region Matrix Transform
		/// <summary>
		/// Convert current Form's 2D space (render textures size)
        /// into U3D's monitor space, based on Window Form coordinate and reflect window form's position on monitor
		/// </summary>
		/// <returns></returns>
		public Matrix4x4 MatrixMonitorToForm() => Matrix4x4.Translate(new Vector3(-Left, +Top + Height, 0));
        public Matrix4x4 MatrixFormToMonitor() => MatrixMonitorToForm().inverse;
		#endregion Matrix Transform

		#region Event Queue
        private enum eEventType
        {
            GotFocus,
            LostFocus,
            Move,

            MouseDown,
            MouseUp,
            MouseClick,
            MouseMove,
            MouseWheel,

            KeyDown,
            KeyUp,
        }
		/// <summary>Cache Window Form events into queue, and wait for unity's main thread update.</summary>
		private class EventPacket
        {
            private System.Action callback;
            public EventPacket(System.Action callback) => this.callback = callback;
            public void Resolve() { this.callback.Invoke(); }
        }
        private class MouseEventPacket : EventPacket
        {
            public MouseEventPacket(System.Action callback) : base(callback) { }
        }
        private Queue<EventPacket> m_Events = new Queue<EventPacket>();

		/// <summary>Dispatch all events by order.</summary>
		internal void ProcessEvents()
        {
            bool hadMouse = false;
            while (m_Events.Count > 0)
            {
                var evt = m_Events.Dequeue();
                if (evt is MouseEventPacket mouse)
                {
					hadMouse = true;
				}
                evt.Resolve();
            }

            if (!hadMouse &&
                dwCamera?.setting != null &&
                dwCamera.setting.CaptureMouseMoveEventOnLoseFocus)
            {
                // Hack : generate global mouse move event on update
                var osEvt = new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0);
				var evt = Convert2MouseEvent(osEvt);
                if (evt.delta.sqrMagnitude > float.Epsilon)
                {
                    // Dispatch when delta had value.
                    Event_MouseMove.TryCatchDispatchEventError(o => o.Invoke(this.hWnd, evt));
                }
			}
        }
		#endregion Event Queue

		#region Mouse Events
		private Vector2 m_LastMonPos;


		private const int WM_MOUSEHWHEEL = 0x020E;
		private const int WHEEL_DELTA = 120;
		private int m_HorizontalWheelDelta = 0;
		private void CacheHorizontalScroll(ref System.Windows.Forms.Message m)
		{
			if (m.Msg != WM_MOUSEHWHEEL)
				return;
			m_HorizontalWheelDelta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
		}

		private PointerEventData Convert2MouseEvent(MouseEventArgs evt)
        {
            var buttonId = evt.Button switch
            {
                MouseButtons.Left => PointerEventData.InputButton.Left,
                MouseButtons.Right => PointerEventData.InputButton.Right,
                MouseButtons.Middle => PointerEventData.InputButton.Middle,
                _ => PointerEventData.InputButton.Middle,
            };

            /// [Fail] Convert MouseEventArgs to PointerEventData
            /// var _mousePos = MatrixOSToMonitor().MultiplyPoint(new Vector3(evt.X, evt.Y, 0f));
            /// Bug : MouseEventArgs will affect by Form's `Left` `Top` values.
            /// therefore the drag process will not work properly.
            /// work around, use <see cref="DwCore.GetOSCursorPos"/> for current mouse position in OS Space.

            var o2m = DwCamera.s_QuickOSToMonitor;
            var m2f = MatrixMonitorToForm();
            var v2i = DwCore.GetOSCursorPos();
			var v3f = new Vector3(v2i.x, v2i.y, 0f);
			var monPos = o2m.MultiplyPoint3x4(v3f); // correct, Cyan (faster)
			var formPos = (m2f * o2m).MultiplyPoint3x4(v3f); // correct, Yellow (faster)
            //var monPos = dwCamera.GetMousePosInMonitorSpace(); // correct
			//var formPos = dwCamera.MatrixOSToForm().MultiplyPoint3x4(osV3f); // correct

			const float PERIOD = 1f;
            if (dwCamera.m_Config.debug.HasFlag(DwCamera.eDebug.DrawClickPosFormSpace))
            {
                DrawPoint(formPos, UnityEngine.Color.yellow, 25f, PERIOD, false);
            }

            if (dwCamera.m_Config.debug.HasFlag(DwCamera.eDebug.DrawClickPosMonitorSpace))
            {
                DrawPoint(monPos, UnityEngine.Color.cyan, 20f, PERIOD, false);
            }

			// movement delta
			var old = m_LastMonPos;
            var next = (Vector2)formPos;
            var delta = next - old;
			m_LastMonPos = next;

			// scroll delta
			var scrollDelta = new Vector2(
                (float)m_HorizontalWheelDelta / WHEEL_DELTA,
                (float)evt.Delta / WHEEL_DELTA);

            // Debug from space ray cast result
            //var r = dwCamera.GetMouseRayInModelSpace();
            //DebugExtend.DrawRay(r.origin, r.direction * 1000f, Color.magenta, 1f, false);
            var camPos = dwCamera.MatrixOSToForm().MultiplyPoint3x4(v3f);
            var ray = dwCamera.linkCamera.ScreenPointToRay(camPos, Camera.MonoOrStereoscopicEye.Mono);
            var rayRst = new RaycastResult()
            {
                screenPosition = (Vector2)monPos,
                worldPosition = ray.origin,
                worldNormal = ray.direction,
            };

            var evtPos = (Vector2)formPos;
			var pEvent = new PointerEventData(EventSystem.current)
            {
                eligibleForClick = true,
                pointerId = 0,
                position = evtPos,
                pressPosition = evtPos,
				// worldPosition = monPos,
				pointerCurrentRaycast = rayRst,

				delta = delta,
                button = buttonId,
                scrollDelta = scrollDelta,
            };
			return pEvent;
        }

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DrawPoint(Vector3 position, UnityEngine.Color color = default(UnityEngine.Color), float scale = 1.0f, float duration = 0, bool depthTest = false)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.DrawRay(position + (Vector3.up * (scale * 0.5f)), -Vector3.up * scale, color, duration, depthTest);
            UnityEngine.Debug.DrawRay(position + (Vector3.right * (scale * 0.5f)), -Vector3.right * scale, color, duration, depthTest);
            UnityEngine.Debug.DrawRay(position + (Vector3.forward * (scale * 0.5f)), -Vector3.forward * scale, color, duration, depthTest);
#endif
        }


        public delegate void PointerEventDelegate(uint hWnd, PointerEventData pointerEventData);
        public event PointerEventDelegate
            Event_MouseClick,
            Event_MouseDown,
            Event_MouseUp,
            Event_MouseMove,
            Event_MouseWheel;

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            var evt = Convert2MouseEvent(e);
            m_Events.Enqueue(new MouseEventPacket(() => Event_MouseDown.TryCatchDispatchEventError(o => o.Invoke(this.hWnd, evt))));
			// Event_MouseDown?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt));
        }
        
        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            var evt = Convert2MouseEvent(e);
            m_Events.Enqueue(new MouseEventPacket(() => Event_MouseUp.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt))));
            //Event_MouseUp?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt));
        }

        private void Form_MouseClick(object sender, MouseEventArgs e)
        {
            var evt = Convert2MouseEvent(e);
			m_Events.Enqueue(new MouseEventPacket(() => Event_MouseClick.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt))));
			//Event_MouseClick?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt));
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            var evt = Convert2MouseEvent(e);
			m_Events.Enqueue(new MouseEventPacket(() => Event_MouseMove.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt))));
			//Event_MouseMove?.TryCatchDispatchEventError(o=> o?.Invoke(this.hWnd, evt));
        }

        private void Form_MouseWheel(object sender, MouseEventArgs e)
        {
            var evt = Convert2MouseEvent(e);
			m_Events.Enqueue(new MouseEventPacket(() => Event_MouseWheel.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt))));
			//Event_MouseWheel?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt));
        }

		private void From_Move(object sender, EventArgs e)
		{
            m_Events.Enqueue(new MouseEventPacket(() => Event_Move.TryCatchDispatchEventError(o => o.Invoke(this.hWnd, e))));
			//Event_Move?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, e));
		}

		#endregion Mouse Events

		#region Key Events

		private bool Convert2KeyEvent(KeyEventArgs e, bool isKeyUp, out Event evt)
        {
            if (!DwUtils.TryWin2KeyMap(e.KeyCode, e.Shift, out var _keyCode, out var _keyChar))
            {
                Debug.LogError($"KeyCode not support KeyCode = {e.KeyCode}, keyData = {e.KeyData}, keyValue = {e.KeyValue}, shift = {e.Shift}");
                evt = null;
                return false;
            }

            var _modifiers = EventModifiers.None;
            if (e.Alt)      _modifiers |= EventModifiers.Alt;
            if (e.Control)  _modifiers |= EventModifiers.Control;
            if (e.Shift)    _modifiers |= EventModifiers.Shift;
            var _type = isKeyUp ? EventType.KeyUp : EventType.KeyDown;

            evt = new UnityEngine.Event
            {
                character   = _keyChar,
                keyCode     = _keyCode,
                modifiers   = _modifiers,
                type        = _type,
            };
            return true;
        }
        public delegate void KeyEventDelegate(uint hWnd, UnityEngine.Event evt);
        public event KeyEventDelegate
            Event_KeyDown,
            Event_KeyUp;
        private void Form_KeyUp(object sender, KeyEventArgs e)
        {
            if (!Convert2KeyEvent(e, isKeyUp: true, out var evt))
                return;
			m_Events.Enqueue(new EventPacket(() => Event_KeyUp.TryCatchDispatchEventError(o => o.Invoke(this.hWnd, evt))));
			// Event_KeyUp?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt));
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Convert2KeyEvent(e, isKeyUp: false, out var evt))
                return;
			m_Events.Enqueue(new EventPacket(() => Event_KeyDown.TryCatchDispatchEventError(o => o.Invoke(this.hWnd, evt))));
			//Event_KeyDown?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, evt));
        }
        #endregion Key Events

        #region Focus Events
        public delegate void EventDelegate(uint hWnd, EventArgs evt);
        public event EventDelegate
            Event_GotFocus,
            Event_LostFocus,
            Event_Move;
        private void Form_LoseFocus(object sender, EventArgs e)
		{
			m_Events.Enqueue(new EventPacket(() => Event_LostFocus.TryCatchDispatchEventError(o => o.Invoke(this.hWnd, e))));
			//Event_LostFocus?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, e));
        }

        private void Form_GotFocus(object sender, EventArgs e)
        {
			m_Events.Enqueue(new EventPacket(() => Event_GotFocus.TryCatchDispatchEventError(o => o.Invoke(this.hWnd, e))));
			//Event_GotFocus?.TryCatchDispatchEventError(o => o?.Invoke(this.hWnd, e));
        }
		#endregion Focus Events

		#region Vision
		private BLENDFUNC blend;
        private void InitVisionSetting()
        {
			blend               = new BLENDFUNC();
			blend.BlendOp       = AC_SRC_OVER;
			blend.BlendFlags    = 0;
			blend.AlphaFormat   = AC_SRC_ALPHA;
		}

		/// <summary>Draw Untiy3d's renderTexture as bitmap on window form</summary>
		/// <param name="bitmap"></param>
		/// <param name="opacity"></param>
		public void Repaint(System.Drawing.Bitmap bitmap, byte opacity)
        {
            var screenDc    = DwCore._GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
            {
                Debug.LogError("Repaint(), screenDc not found.");
                return;
            }
			var memDc       = DwCore._CreateCompatibleDC(screenDc);
            if (memDc == IntPtr.Zero)
			{
				Debug.LogError("Repaint(), memDc not found.");
				return;
			}
			var hBitmap     = bitmap.GetHbitmap(System.Drawing.Color.FromArgb(0));
			if (hBitmap == IntPtr.Zero)
            {
                Debug.LogError("Repaint(), hBitmap not found.");
                return;
			}
			var oldBitmap   = DwCore._SelectObject(memDc, hBitmap);
			if (oldBitmap == IntPtr.Zero)
			{
				Debug.LogError("Repaint(), oldBitmap not found.");
				return;
			}
			var size        = new POINT(bitmap.Width, bitmap.Height);
            var pointSource = new POINT(0, 0);
            var topPos      = new POINT(Left, Top);

            blend.SourceConstantAlpha = opacity;
            if (Handle == IntPtr.Zero)
			{
                Debug.LogWarning("DwForm::Repaint() : Handle is null", dwCamera);
				return;
            }
            const int crKey = 0;
			DwCore._UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, crKey, ref blend, ULW_ALPHA);

            if (hBitmap != IntPtr.Zero)
            {
                DwCore._SelectObject(memDc, oldBitmap);
                DwCore._DeleteObject(hBitmap);
            }
            DwCore._DeleteDC(memDc);
            DwCore._ReleaseDC(IntPtr.Zero, screenDc);
        }
		#endregion Vision

		#region Override Window Proc
		/// Hack to make window-form draggable,
		/// but we want to make it draggable only, if user hold the *right* mouse button.
		/// <see cref="FormSetting.dragMethod"/>
		protected override void WndProc(ref Message m)
        {
			// m.Result = (IntPtr)1; //HTCAPTION

			// Handle mouse wheel scroll event
			CacheHorizontalScroll(ref m);

            base.WndProc(ref m);
        }

        /// <summary>Allow window to be transparent</summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                
                /// Allow window to be transparent
                /// <see cref="WinExStyle"/>
                cp.ExStyle |= (int)WinExStyle.LAYERED;

				/// Ensure the window did not have following:
				/// caption, border, scroll bar, toolbar, status bar, etc.
				/// erase all default window setting by assign it to 0.
                /// <see cref="WinStyle"/>
				cp.Style = 0; // (int)WinStyle.OVERLAPPED;
				return cp;
            }
        }
		#endregion Override Window Proc
	}
}