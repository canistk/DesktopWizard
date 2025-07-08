using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace Gaia
{
    public class UIFileView : ViewBase<UIFileCtrl, FileInfo>
	{
		[SerializeField] Image m_Icon;
		[SerializeField] UIText m_Label;
		[SerializeField] UIButton m_Button;

		private void Reset()
		{
			m_Icon = GetComponentInChildren<Image>();
			m_Label = GetComponentInChildren<UIText>();
			m_Button = GetComponentInChildren<UIButton>();
		}

		protected override void OnViewUpdate(FileInfo data)
		{
			if (m_Label)
			{
				m_Label.Text = data.Name;
			}
		}
    }
}