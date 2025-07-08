using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace Gaia
{
	public class UIFolderView : ViewBase<UIFolderCtrl, DirectoryInfo>
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

		protected override void OnViewUpdate(DirectoryInfo data)
		{
			if (m_Label)
			{
				m_Label.Text = data.Name;
			}
		}
	}
}