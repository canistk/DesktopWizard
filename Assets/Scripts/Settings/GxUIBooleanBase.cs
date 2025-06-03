using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Gaia
{
    public abstract class GxUIBooleanBase<T> : GxUISetting<T>
	{
		[Header("Toggle(s)")]
		public ToggleGroup m_ToggleGroup;
		public Toggle m_On, m_Off;

		private void Reset()
		{
			AutoBind();
		}

		protected virtual void Start()
		{
			BindBtns();
		}

		protected virtual void OnDestroy()
		{
		}

		private void AutoBind()
		{
			if (m_ToggleGroup == null)
				m_ToggleGroup = GetComponentInChildren<ToggleGroup>(true);
			if (m_On == null || m_Off == null)
			{
				var toggles = GetComponentsInChildren<Toggle>(true);
				const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
				if (m_On == null)
				{
					m_On = toggles.FirstOrDefault(o => o.name.Contains("on", IGNORE));
				}
				if (m_Off == null)
				{
					m_Off = toggles.FirstOrDefault(o => o.name.Contains("off", IGNORE));
				}
			}
		}
		private void BindBtns()
		{
			AutoBind();
			if (m_ToggleGroup == null || m_On == null || m_Off == null)
				throw new System.Exception("Unable to init, UI component not found.");
			m_On.SetIsOnWithoutNotify(false);
			m_Off.SetIsOnWithoutNotify(false);

			m_On.group = m_Off.group = m_ToggleGroup;
			var t = GetIndex() == 0 ? m_Off : m_On;
			t.SetIsOnWithoutNotify(true);

			m_On.onValueChanged.AddListener(OnToggleClicked);
			m_Off.onValueChanged.AddListener(OnToggleClicked);
		}

		private void OnToggleClicked(bool isOn)
		{
			if (!isOn)
				return;
			var selected = GetSelectedIndex();
			SetIndex(selected);
		}

		protected int GetSelectedIndex()
		{
			if (m_ToggleGroup == null)
				return -1;
			if (m_ToggleGroup.GetFirstActiveToggle() == m_On)
				return 1;
			return 0;
		}

		protected override void OnSourceUpdated(int index, string name, T value)
		{
			var t = index == 0 ? m_Off : m_On;
			t.SetIsOnWithoutNotify(true);
		}
	}
}