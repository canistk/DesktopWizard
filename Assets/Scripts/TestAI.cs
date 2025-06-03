using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deepseek;
using Obi;
using Newtonsoft.Json;
namespace Gaia
{
    public class TestAI : MonoBehaviour
    {
		//[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		//public static void AutoStartUp() { }
		[SerializeField] DeepseekAgent m_Agent;
		public string m_SystemMsg = "";

		public List<MyMsg> msgs = new List<MyMsg>();

		public enum eMsgType
		{
			User,
			Assistant,
		}
		[System.Serializable]
		public class MyMsg
		{
			public eMsgType type;
			public string content;
		}

		[ContextMenu("Fetched Models")]
		private void FetchedModels()
		{
			if (m_Agent == null)
				return;
			m_Agent.ListModels((data) => {
				Debug.Log($"Fetched Models: {data}");
			});
		}

		[SerializeField] bool m_UseSteam = false;

		[ContextMenu("Send Chat")]
		private void SendChat()
		{
			if (m_Agent == null)
				return;

			var chat = new Chat(eDeepseekModel.chat, m_UseSteam);

			if (!string.IsNullOrEmpty(m_SystemMsg))
				chat.messages.Add(new SystemMessage(m_SystemMsg));

			for (int i = 0; i < msgs.Count; i++)
			{
				if (string.IsNullOrEmpty(msgs[i].content))
					continue;
				Message m = msgs[i].type == eMsgType.User ?
					new UserMessage(msgs[i].content) :
					new AssistantMessage("Assistant", msgs[i].content, false, null);
				chat.messages.Add(m);
			}

			m_Agent.Chat(chat,
			(rst) => {
				var json = Deepseek.Utils.ToJson(rst);
				Debug.Log($"Received {json}");
				var cnt = rst.choices.Length;
				var i = cnt;
				while (i --> 0)
				{
					if (rst.choices[i].message is AssistantMessage)
					{
						var am = rst.choices[i].message as AssistantMessage;
						
						msgs.Add(new MyMsg { type = eMsgType.Assistant, content = am.content });

						msgs.Add(new MyMsg { type = eMsgType.User, content = "" });
						break;
					}
				}
			},
			Debug.LogError,
			Debug.Log);
		}

		[ContextMenu("Test Json")]
		private void Test()
		{
			string json = "{\"id\":\"35fe6858-03f2-4afa-8cd6-32da57e577ba\",\"object\":\"chat.completion\",\"created\":1738919329,\"model\":\"deepseek-chat\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"Hello! How can I assist you today?\"},\"logprobs\":null,\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":4,\"completion_tokens\":11,\"total_tokens\":15,\"prompt_tokens_details\":{\"cached_tokens\":0},\"prompt_cache_hit_tokens\":0,\"prompt_cache_miss_tokens\":4},\"system_fingerprint\":\"fp_3a5770e1b4\"}\r\n";

			var rst = Deepseek.Utils.FromJson<ChatCompletion>(json);
			Debug.Log("Finish");
		}
	}
}