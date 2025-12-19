using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DesktopWizard
{
	/// <summary>
	/// A wrapper class that acts as a middle layer between DwCamera and DwForm.
	/// Provides all DwForm-related functionality to DwCamera.
	/// </summary>
	public class DwWinWrapper
	{
		private DwForm _Form;
		private DwCamera m_Owner;

		/// <summary>Get the underlying DwForm instance.</summary>
		public DwForm dwForm => _Form;

		/// <summary>Get the owner DwCamera instance.</summary>
		public DwCamera owner => m_Owner;

		// For Drag Move
		private class DragDwFormInfo
		{
			public bool isDragging { get; private set; }
			public int offsetX;
			public int offsetY;
			public void StartDrag(DwForm from)
			{
				isDragging = true;

				var point = DwCore.GetOSCursorPos();
				offsetX = from.Left - point.x;
				offsetY = from.Top - point.y;
			}
			public void Reset()
			{
				isDragging = false;
				offsetX = 0;
				offsetY = 0;
			}
		}

		private DragDwFormInfo m_DragFormInfo = new DragDwFormInfo();
		public bool IsFormDragging => m_DragFormInfo?.isDragging ?? false;

		public DwWinWrapper(DwCamera owner)
		{
			m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		#region Form Lifecycle
		/// <summary>Create the Windows Form.</summary>
		public void CreateForm(FormSetting setting)
		{
			if (_Form != null)
				throw new Exception("Form already created.");

			_Form = new DwForm(m_Owner);
			AddEvents(_Form);
			_Form.Show();
		}

		/// <summary>Destroy the Windows Form.</summary>
		public void DestroyForm()
		{
			if (_Form != null)
			{
				RemoveEvents(_Form);
				_Form.Close();
			}
			_Form = null;
		}

		/// <summary>Check if form is created and valid.</summary>
		public bool IsFormValid => _Form != null;
		#endregion Form Lifecycle

		#region Properties
		/// <summary>Window title.</summary>
		public string Title
		{
			get => _Form == null ? string.Empty : _Form.Text;
			set
			{
				if (_Form != null)
					_Form.Text = value;
			}
		}

		/// <summary>Left position in OS space.</summary>
		public int Left
		{
			get => _Form?.Left ?? 0;
			set
			{
				if (_Form != null)
					_Form.Left = value;
			}
		}

		/// <summary>Top position in OS space.</summary>
		public int Top
		{
			get => _Form?.Top ?? 0;
			set
			{
				if (_Form != null)
					_Form.Top = value;
			}
		}

		/// <summary>Width in OS space.</summary>
		public int Width
		{
			get => _Form?.Width ?? 0;
			set
			{
				if (_Form != null)
					_Form.Width = value;
			}
		}

		/// <summary>Height in OS space.</summary>
		public int Height
		{
			get => _Form?.Height ?? 0;
			set
			{
				if (_Form != null)
					_Form.Height = value;
			}
		}

		/// <summary>TopMost flag.</summary>
		public bool TopMost
		{
			get => _Form?.TopMost ?? false;
			set
			{
				if (_Form != null)
					_Form.TopMost = value;
			}
		}

		/// <summary>Visible flag.</summary>
		public bool Visible => _Form?.Visible ?? false;

		/// <summary>Focused flag.</summary>
		public bool Focused => _Form?.Focused ?? false;

		/// <summary>Window handle.</summary>
		public uint hWnd => _Form?.hWnd ?? 0u;

		/// <summary>Form size.</summary>
		public System.Drawing.Size Size
		{
			get => _Form?.Size ?? default;
			set
			{
				if (_Form != null)
					_Form.Size = value;
			}
		}

		/// <summary>Window handle as IntPtr.</summary>
		public IntPtr Handle => _Form?.Handle ?? IntPtr.Zero;
		#endregion Properties

		#region Position and Size Methods
		/// <summary>Get position in OS space.</summary>
		public Vector2Int GetOsPos()
		{
			if (_Form == null)
				return Vector2Int.zero;
			return new Vector2Int(_Form.Left, _Form.Top);
		}

		/// <summary>Set position in OS space.</summary>
		public void SetOsPos(Vector2Int pos, int width, int height)
		{
			if (_Form != null)
				_Form.SetBounds(pos.x, pos.y, width, height);
		}

		/// <summary>Get size in OS space.</summary>
		public Vector2Int GetOsSize()
		{
			if (_Form == null)
				return Vector2Int.zero;
			return new Vector2Int(_Form.Width, _Form.Height);
		}

		/// <summary>Set size in OS space.</summary>
		public void SetOsSize(Vector2Int size, int left, int top)
		{
			if (_Form != null)
				_Form.SetBounds(left, top, size.x, size.y);
		}
		#endregion Position and Size Methods

		#region Matrix Transform Methods
		/// <summary>Matrix to transform from Monitor space to Form space.</summary>
		public Matrix4x4 MatrixMonitorToForm()
		{
			return _Form?.MatrixMonitorToForm() ?? Matrix4x4.identity;
		}

		/// <summary>Matrix to transform from Form space to Monitor space.</summary>
		public Matrix4x4 MatrixFormToMonitor()
		{
			return _Form?.MatrixFormToMonitor() ?? Matrix4x4.identity;
		}
		#endregion Matrix Transform Methods

		#region Window Info
		/// <summary>Try to get window information.</summary>
		public bool TryGetWindowInfo(out WindowInfo windowInfo)
		{
			if (_Form == null)
			{
				windowInfo = default;
				return false;
			}

			var id = (uint)_Form.Handle;
			return DwCore.TryGetWindowById(id, out windowInfo);
		}

		/// <summary>Get screen width.</summary>
		public int ScreenWidth => _Form == null ? -1 : System.Windows.Forms.Screen.GetBounds(_Form).Width;

		/// <summary>Get screen height.</summary>
		public int ScreenHeight => _Form == null ? -1 : System.Windows.Forms.Screen.GetBounds(_Form).Height;
		#endregion Window Info

		#region Event Management
		public delegate void PointerEventDelegate(uint hWnd, PointerEventData evt);
		public event PointerEventDelegate
			EVENT_MouseMove, EVENT_MouseWheel,
			EVENT_MouseDown, EVENT_MouseUp;

		public delegate void KeyEventDelegate(uint hWnd, Event e);
		public event KeyEventDelegate
			EVENT_KeyDown,
			EVENT_KeyUp;

		public delegate void EventDelegate(uint hWnd, EventArgs evt);
		public event EventDelegate
			EVENT_GotFocus,
			EVENT_LostFocus,
			EVENT_Move;

		public event Action EVENT_FormClosing;

		private void AddEvents(DwForm f)
		{
			f.Event_MouseDown += Form_MouseDown;
			f.Event_MouseUp += Form_MouseUp;
			f.Event_MouseMove += Form_MouseMove;
			f.Event_MouseWheel += Form_MouseWheel;
			f.Event_KeyDown += Form_KeyDown;
			f.Event_KeyUp += Form_KeyUp;
			f.Event_GotFocus += Form_GotFocus;
			f.Event_LostFocus += Form_LostFocus;
			f.Event_Move += Form_Move;
			f.FormClosing += Form_Closing;
		}

		private void RemoveEvents(DwForm f)
		{
			f.Event_MouseDown -= Form_MouseDown;
			f.Event_MouseUp -= Form_MouseUp;
			f.Event_MouseMove -= Form_MouseMove;
			f.Event_MouseWheel -= Form_MouseWheel;
			f.Event_KeyDown -= Form_KeyDown;
			f.Event_KeyUp -= Form_KeyUp;
			f.Event_GotFocus -= Form_GotFocus;
			f.Event_LostFocus -= Form_LostFocus;
			f.Event_Move -= Form_Move;
			f.FormClosing -= Form_Closing;
		}

		private void Form_MouseDown(uint hWnd, PointerEventData evt) => EVENT_MouseDown?.Invoke(hWnd, evt);
		private void Form_MouseUp(uint hWnd, PointerEventData evt) => EVENT_MouseUp?.Invoke(hWnd, evt);
		private void Form_MouseMove(uint hWnd, PointerEventData evt) => EVENT_MouseMove?.Invoke(hWnd, evt);
		private void Form_MouseWheel(uint hWnd, PointerEventData evt) => EVENT_MouseWheel?.Invoke(hWnd, evt);
		private void Form_KeyDown(uint hWnd, Event e) => EVENT_KeyDown?.Invoke(hWnd, e);
		private void Form_KeyUp(uint hWnd, Event e) => EVENT_KeyUp?.Invoke(hWnd, e);
		private void Form_GotFocus(uint hWnd, EventArgs evt) => EVENT_GotFocus?.Invoke(hWnd, evt);
		private void Form_LostFocus(uint hWnd, EventArgs evt) => EVENT_LostFocus?.Invoke(hWnd, evt);
		private void Form_Move(uint hWnd, EventArgs evt) => EVENT_Move?.Invoke(hWnd, evt);
		private void Form_Closing(object sender, EventArgs e) => EVENT_FormClosing?.Invoke();
		#endregion Event Management

		#region Form Operations
		/// <summary>Process queued form events.</summary>
		public void ProcessEvents()
		{
			_Form?.ProcessEvents();
		}

		/// <summary>Repaint the form with a bitmap.</summary>
		public void Repaint(System.Drawing.Bitmap bitmap, byte opacity)
		{
			_Form?.Repaint(bitmap, opacity);
		}

		/// <summary>Start dragging the form.</summary>
		public void StartDrag()
		{
			if (_Form != null)
				m_DragFormInfo.StartDrag(_Form);
		}

		/// <summary>Reset drag state.</summary>
		public void ResetDrag()
		{
			m_DragFormInfo.Reset();
		}

		/// <summary>Update form position during dragging.</summary>
		public void UpdateDragPosition()
		{
			if (_Form != null && m_DragFormInfo.isDragging)
			{
				var cursor = DwCore.GetOSCursorPos();
				_Form.Left = cursor.x + m_DragFormInfo.offsetX;
				_Form.Top = cursor.y + m_DragFormInfo.offsetY;
			}
		}
		#endregion Form Operations
	}
}
