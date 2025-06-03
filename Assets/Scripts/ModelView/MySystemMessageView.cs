using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deepseek;
namespace Gaia
{
    public class MySystemMessageView : MyMessageView<SystemMessage>
	{
		[SerializeField] UIText m_Role;
		[SerializeField] UIText m_Content;

		protected override void OnViewUpdate(SystemMessage data)
		{
			if (m_Role)
				m_Role.Text = data.role;
			if (m_Content)
				m_Content.Text = data.content;
		}
	}
}