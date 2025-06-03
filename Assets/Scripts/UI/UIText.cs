using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace Gaia
{
	//[RequireComponent(typeof(TextMeshProUGUI))]
	public class UIText : MonoBehaviour
	{
		[SerializeField] TMP_Text m_Text;
		public string Text
		{
			get => m_Text.text;
			set => m_Text.text = value;
		}
		private void Reset()
		{
			m_Text = GetComponentInChildren<TMP_Text>(true);
		}

		private void Awake()
		{
			if (m_Text == null)
				m_Text = GetComponentInChildren<TMP_Text>(true);
		}

		public void SetColor(Color color)
		{
			if (m_Text)
				m_Text.color = color;
		}
	}
}