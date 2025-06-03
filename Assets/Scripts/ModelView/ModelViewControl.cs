using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gaia
{
	public abstract class BindingBase : MonoBehaviour
	{
		public event System.Action<BindingBase> EVENT_DataUpdateBase;

		public abstract void Assign(object data, bool forceUpdate = false);
		public abstract bool CanHandle(object raw);

		protected void EVENT_DataUpdateBase_Invoke() { EVENT_DataUpdateBase?.Invoke(this); }

	}

	/// <summary>
	/// Reference from <see cref="SceneViewBase"/>
	/// </summary>
	public abstract class Binding<DATA> : BindingBase
		where DATA : class
	{
		private DATA _data;
		public DATA data
		{
			get => _data;
			private set
			{
				if (_data == value)
					return;

				if (_data != null && _data is IDataUpdatable old)
					old.EVENT_Updated -= TriggerUpdate;
				_data = value;
				if (_data != null && _data is IDataUpdatable src)
					src.EVENT_Updated += TriggerUpdate;
			}
		}

		public event System.Action<DATA> EVENT_DataUpdate;
		public override void Assign(object data, bool forceUpdate = false)
		{
			Assign(data as DATA, forceUpdate);
		}
		public void Assign(DATA data, bool forceUpdate = false)
		{
			if (this.data == data && !forceUpdate)
			{
				return;
			}
			if (this.data != null)
				OnUnassign(this.data);
			this.data = data;
			OnAssign(data);
			TriggerUpdate();
		}

		public override bool CanHandle(object raw)
		{
			return raw is DATA;
		}

		protected virtual void OnUnassign(DATA data) { }
		protected virtual void OnAssign(DATA data) { }

		protected void TriggerUpdate()
		{
			EVENT_DataUpdate?.Invoke(data);
			EVENT_DataUpdateBase_Invoke();
		}
	}

	public abstract class ViewBase<BIND, DATA> : ViewBaseBase<BIND, DATA>
		where BIND : Binding<DATA>
		where DATA : class
	{
		protected virtual void OnEnable()
		{
			if (ctrl == null)
			{
				// TODO: should not had this cases, change to exception.
				Debug.LogError($"{typeof(BIND)} not found. ${transform}", this);
				return;
			}
			InternalDataUpdate(data);
			ctrl.EVENT_DataUpdate += InternalDataUpdate;
		}

		protected virtual void OnDisable()
		{
			if (ctrl == null) return;

			ctrl.EVENT_DataUpdate -= InternalDataUpdate;
			_ctrl = null;
		}
	}

	public abstract class ViewBaseBase<BIND, DATA> : MonoBehaviour
		where BIND : Binding<DATA>
		where DATA : class
	{
		protected BIND _ctrl;
		protected BIND ctrl
		{
			get
			{
				if (_ctrl == null)
					_ctrl = GetComponentInParent<BIND>(true);
				return _ctrl;
			}
		}
		public BIND control => ctrl;
		protected DATA data => ctrl?.data;
		protected virtual void Awake() { }

		protected virtual void OnDestroy() { }

		protected void InternalDataUpdate(DATA data)
		{
			try
			{
				if (data != null)
					OnViewUpdate(data);
				else
					OnViewUpdateInvalid(data);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		protected virtual void OnViewUpdateInvalid(object dataRef) { }

		protected abstract void OnViewUpdate(DATA data);
	}

	/// <summary>
	/// A Binding wrapper design for <see cref="AxUISpawnRoot"/> in order to carry the virtual selection data
	/// for those non-exist UI, due to optimization.
	/// since <see cref="AxUISpawnRoot"/> handle the mess UI element(s) spawn feature, the element out of viewport will be auto despawn,
	/// therefore the UI's information will be missing the handler, such as { Toggle, Selection, UI change...etc }
	/// the <see cref="AxDataWrapper{SRC}"/> served as the UI data container.
	/// </summary>
	/// <typeparam name="CTRL"></typeparam>
	/// <typeparam name="DATA"></typeparam>
	/// <typeparam name="SRC"></typeparam>
	public abstract class AxCtrlWrapper<CTRL, DATA, SRC> : Binding<DATA>
		where CTRL : Binding<DATA>
		where DATA : AxDataWrapper<SRC>
		where SRC : class, IDataUpdatable
	{
		[SerializeField] BindingBase m_Ctrl;

		protected virtual void Awake()
		{
			if (m_Ctrl == null)
				m_Ctrl = GetComponentInChildren<Binding<SRC>>(true);
		}

		protected override void OnAssign(DATA data)
		{
			base.OnAssign(data);
			if (m_Ctrl)
			{
				m_Ctrl.Assign(data.source);
			}
		}
	}

	/// <summary>
	/// implement a common <see cref="UIButton"/> provide <see cref="BindButton(AxCtrlWrapper_Button{CTRL, DATA, SRC}.RedirectClick)"/>
	/// for easy binding the UI callback toward the owner.
	/// </summary>
	/// <typeparam name="CTRL"></typeparam>
	/// <typeparam name="DATA"></typeparam>
	/// <typeparam name="SRC"></typeparam>
	public abstract class AxCtrlWrapper_Button<CTRL, DATA, SRC> : AxCtrlWrapper<CTRL, DATA, SRC>
		where CTRL : Binding<DATA>
		where DATA : AxDataWrapper<SRC>
		where SRC : class, IDataUpdatable,
		IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] UIButton m_UIButton;
		protected override void OnUnassign(DATA data)
		{
			base.OnUnassign(data);
			UnBind();
		}

		protected override void Awake()
		{
			base.Awake();
			if (m_UIButton)
				m_UIButton.EVENT_OnClick += OnClick;
		}

		private void OnDestroy()
		{
			UnBind();
			if (m_UIButton)
				m_UIButton.EVENT_OnClick -= OnClick;
		}

		public delegate void RedirectClick(AxCtrlWrapper<CTRL, DATA, SRC> ctrl, DATA data);
		public RedirectClick clickCallback = null;
		public void BindButton(RedirectClick clickCallback)
		{
			this.clickCallback = clickCallback;
		}
		private void UnBind()
		{
			this.clickCallback = null;
		}
		private void OnClick()
		{
			this.clickCallback?.Invoke(this, data);
		}
	}

	public abstract class AxDataWrapper
	{
		public abstract object GetSource();
	}
	/// <summary>
	/// A data wrapper class to handle UI information, and redirect the source data update event.
	/// provide public <see cref="TriggerUpdate"/> for UI update purpose.
	/// </summary>
	/// <typeparam name="SRC"></typeparam>
	public abstract class AxDataWrapper<SRC> : AxDataWrapper, IDataUpdatable
		where SRC : IDataUpdatable
	{
		public readonly SRC source;
		public override object GetSource() => source;
		#region Constructor
		public AxDataWrapper(SRC source)
		{
			this.source = source;
			this.source.EVENT_Updated += TriggerUpdate;
		}
		~AxDataWrapper()
		{
			this.source.EVENT_Updated -= TriggerUpdate;
		}
		#endregion Constructor

		#region IDataUpdatable
		public event System.Action EVENT_Updated;
		public void TriggerUpdate()
		{
			EVENT_Updated?.Invoke();
		}
		public void Refresh()
		{
			TriggerUpdate();
		}
		#endregion IDataUpdatable

		#region Usage Example
		public bool IsSelected { get; private set; } = false;
		public void SetSelect(bool selected)
		{
			if (this.IsSelected == selected)
				return;
			IsSelected = selected;
			TriggerUpdate();
		}
		#endregion Usage Example
	}

	public interface IDataUpdatable
	{
		public event System.Action EVENT_Updated;
	}
}