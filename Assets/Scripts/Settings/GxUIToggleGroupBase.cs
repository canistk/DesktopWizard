using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Gaia
{
    public abstract class GxUIToggleGroupBase<T> : GxUISetting<T>
    {
		[Header("Toggle(s)")]
		public GameObject m_TogglePrefab;
		public Transform m_SpawnLayer;
		public ToggleGroup m_ToggleGroup;
		protected struct ToggleInfo
		{
			public GameObject gameObject;
			public Toggle toggle;
			public UIText label;
			public ToggleInfo(GameObject go, Toggle toggle, UIText label)
			{
				this.gameObject = go;
				this.toggle = toggle;
				this.label = label;
			}
		}
		protected List<ToggleInfo> m_Toggles = new List<ToggleInfo>();

		protected virtual void Reset()
		{
			m_ToggleGroup = GetComponentInChildren<ToggleGroup>();
			if (m_ToggleGroup != null && m_SpawnLayer == null)
				m_SpawnLayer = m_ToggleGroup.transform;
		}
		protected virtual void Start()
		{
			CleanPreview();
			GenerateOptions();
		}

		protected virtual void OnDestroy()
		{
			DespawnAll();
		}

		private bool m_CleanPreview = false;
		private void CleanPreview()
		{
			if (m_CleanPreview)
				return;
			if (m_SpawnLayer == null)
				return;
			for (int i = 0; i < m_SpawnLayer.childCount; ++i)
				m_SpawnLayer.GetChild(i).gameObject.SetActive(false);
			m_CleanPreview = true;
		}

		private void GenerateOptions()
		{
			DespawnAll();

			var names = GetOptionNames();
			for (int i = 0; i < names.Length; ++i)
			{
				var name = names[i];
				if (string.IsNullOrEmpty(name))
					continue;

				var token = GameObject.Instantiate(m_TogglePrefab, m_SpawnLayer);
				var toggle = token.GetComponent<Toggle>();
				if (toggle == null)
					continue;
				toggle.SetIsOnWithoutNotify(false);
				toggle.group = m_ToggleGroup;
				toggle.onValueChanged.RemoveAllListeners();
				toggle.onValueChanged.AddListener(OnToggleClicked);
				var label = token.GetComponent<UIText>();
				if (label != null)
					label.Text = name;

				token.SetActive(true);
				m_Toggles.Add(new ToggleInfo(token, toggle, label));
			}

			var selected = GetIndex();
			m_Toggles[selected].toggle.SetIsOnWithoutNotify(true);
		}

		private void DespawnAll()
		{
			for (int i = 0; i < m_Toggles.Count; ++i)
			{
				if (m_Toggles[i].toggle == null)
					continue;
				m_Toggles[i].toggle.onValueChanged.RemoveListener(OnToggleClicked);
				Destroy(m_Toggles[i].gameObject);
			}
			m_Toggles.Clear();
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
			var active = m_ToggleGroup.GetFirstActiveToggle();
			if (active == null)
				return -1;
			for (int i = 0; i < m_Toggles.Count; ++i)
			{
				if (m_Toggles[i].toggle != active)
					continue;
				return i;
			}
			return -1;
		}

		protected override void OnSourceUpdated(int index, string name, T value)
		{
			try
			{
				m_Toggles[index].toggle.SetIsOnWithoutNotify(true);
			}
			catch (System.Exception ex)
			{
				Debug.LogException(ex, this);
			}
		}
	}
}