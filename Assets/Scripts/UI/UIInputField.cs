using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Gaia
{
    public class UIInputField : MonoBehaviour
    {
		[SerializeField] TMP_InputField m_InputField;
		[SerializeField] eSubmitHotkey m_SubmitHotkey = eSubmitHotkey.CtrlEnter;

		private enum eSubmitHotkey
		{
			Enter,
			CtrlEnter,
			ShiftEnter,
			AltEnter,
		}

		public TMP_InputField src => m_InputField;
		public event System.Action<string> EVENT_Submit;

		public string Text
		{
			get
			{
				return m_InputField.text;
			}
			set
			{
				m_InputField.text = value;
			}
		}

		private void Reset()
		{
			m_InputField = GetComponentInChildren<TMP_InputField>(true);
		}

		private void Awake()
		{
			if (m_InputField == null)
				m_InputField = GetComponentInChildren<TMP_InputField>(true);
			// m_InputField.onSubmit.AddListener(OnSubmit);
			// m_InputField.onEndEdit.AddListener(OnSubmit);
		}

		private void Update()
		{
			if (!m_InputField.isFocused)
				return;
			var isEnter = Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter);
			if (!isEnter)
				return;
			var ctrl	= Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
			var shift	= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			var alt		= Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

			switch (m_SubmitHotkey)
			{
				case eSubmitHotkey.Enter:					OnSubmit(m_InputField.text); break;
				case eSubmitHotkey.CtrlEnter:	if (ctrl)	OnSubmit(m_InputField.text); break;
				case eSubmitHotkey.ShiftEnter:	if (shift)	OnSubmit(m_InputField.text); break;
				case eSubmitHotkey.AltEnter:	if (alt)	OnSubmit(m_InputField.text); break;
			}
		}

		private void OnSubmit(string text)
		{
			EVENT_Submit.TryCatchDispatchEventError(o => o.Invoke(text));
		}

		public void Clear()
		{
			m_InputField.text = string.Empty;
		}

		public void SetColor(Color color)
		{
			if (m_InputField)
				m_InputField.textComponent.color = color;
		}
	}
}