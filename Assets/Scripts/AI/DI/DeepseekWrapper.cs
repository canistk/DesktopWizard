using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deepseek;
using System.Threading.Tasks;
namespace AI
{
    //using Deepseek = Deepseek.DeepseekAgent;
	public class DeepseekWrapper : IOpenAILike
	{
		string IOpenAILike.modelName => throw new System.NotImplementedException();

		Task<AI.Conversation> IOpenAILike.Chat(AI.MessageBase[] messages)
		{
			throw new System.NotImplementedException();
		}

		Task<string[]> IOpenAILike.ListModels()
		{
			throw new System.NotImplementedException();
		}

		Deepseek.Message ToDeepseek(AI.MessageBase m)
		{
			switch (m.role)
			{
				case "system":
					{
						var s = m as AI.SystemMessage;
						return new Deepseek.SystemMessage(s.content);
					}
				case "user":
					{
						var u = m as AI.UserMessage;
						return new Deepseek.UserMessage(u.name, u.content);
					}
				case "assistant":
					{
						var a = m as AI.AssistantMessage;
						return new Deepseek.AssistantMessage(a.name, a.content);
					}
				default:
					throw new System.NotSupportedException($"Unknown role: {m.role}");
			}
		}

		AI.MessageBase ToAI(Deepseek.Message m)
		{
			switch(m.role)
			{
				case "system":
				{
					var s = m as Deepseek.SystemMessage;
					return new AI.SystemMessage(s.content);
				}
				case "user":
				{
					var u = m as Deepseek.UserMessage;
					return new AI.UserMessage(u.name, u.content);
				}
				case "assistant":
				{
					var a = m as Deepseek.AssistantMessage;
					return new AI.AssistantMessage(a.name, a.content);
				}
				case "tool":
				{
					var t = m as Deepseek.ToolMessage;
					//throw new System.NotSupportedException();
					Debug.LogError($"Not supported {m.role}");
						break;
				}
				default:
					throw new System.NotSupportedException($"Not supported {m.role}");
			}
			return null;
		}
	}
}