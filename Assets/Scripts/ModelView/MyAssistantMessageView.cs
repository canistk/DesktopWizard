using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deepseek;
namespace Gaia
{
	public class MyAssistantMessageView : MyMessageView<AssistantMessage>
	{
		[SerializeField] UIText m_Role;
		[SerializeField] UIText m_Content;
		[SerializeField] UIText m_Name;
		[SerializeField] UIText m_ReasoningContent;

		protected override void OnViewUpdate(AssistantMessage data)
		{
			if (m_Role)
				m_Role.Text = data.role;
			if (m_Content)
				m_Content.Text = data.content;
			if (m_Name)
				m_Name.Text = data.name;
			if (m_ReasoningContent)
				m_ReasoningContent.Text = data.reasoningContent;
		}
	}
}