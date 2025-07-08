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
		protected override void OnViewUpdate(DirectoryInfo data)
		{
			if (m_Label)
			{
				m_Label.Text = data.Name;
			}
		}
	}
}