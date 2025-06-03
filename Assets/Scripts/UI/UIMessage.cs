using MPUIKIT;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class UIMessage : MonoBehaviour
    {
        [SerializeField] private UIText m_Text;
		[SerializeField] private GameObject m_LeftSpace;
		[SerializeField] private GameObject m_RightSpace;
		[SerializeField] private eAlign m_Align;


		private void OnValidate()
		{
			SetAlignment(m_Align);
		}

		public string Text
		{
			get => m_Text.Text;
			set => m_Text.Text = value;
		}

		public enum eAlign
		{
			Left,
			Right,
			Center,
			Fill,
		}

		public void SetAlignment(eAlign align)
		{
			m_LeftSpace.SetActive(
				align == eAlign.Right ||
				align == eAlign.Center
			);
			
			m_RightSpace.SetActive(
				align == eAlign.Left ||
				align == eAlign.Center
			);
		}

	}
}