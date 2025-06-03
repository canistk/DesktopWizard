using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deepseek;
namespace Gaia
{
	public class MyToolMessageView : MyMessageView<ToolMessage>
	{
		[SerializeField] UIText m_Role;
		[SerializeField] UIText m_Content;
		[SerializeField] UIText m_ToolCallId;

		protected override void OnViewUpdate(ToolMessage data)
		{
			if (m_Role)
				m_Role.Text = data.role;
			if (m_Content)
				m_Content.Text = data.content;
			if (m_ToolCallId)
				m_ToolCallId.Text = data.tool_call_id;
		}
	}
}