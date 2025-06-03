using Gaia;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kit2;
using Kit2.Pooling;
using System.IO;
using System.Threading;
using Deepseek;
using Deepseek.Functions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;
namespace Gaia
{
	using eAlign = UIMessage.eAlign;
	[RequireComponent(typeof(ObjectPool))]
	public class ChatAgentV1 : MonoBehaviour
	{
		[SerializeField] UIMessage m_AiMsgPrefab = null;
		[SerializeField] UIMessage m_MyMsgPrefab = null;
		[SerializeField] UIInputField m_InputField = null;
		[SerializeField] ScrollRect m_ScrollRect = null;
		Transform m_SpawnLayer => m_ScrollRect?.content ?? null;
		[SerializeField] DeepseekAgent m_AI = null;

		[SerializeField] bool m_Steam = true;
		[SerializeField] string m_SaveKey = "TestSave0";

		private List<Message> m_History = null;
		private HashLock<object> m_Interact = new HashLock<object>(true);
		private ObjectPool m_Pool = null;
		private ObjectPool pool
		{
			get
			{
				if (m_Pool == null)
					m_Pool = GetComponent<ObjectPool>();
				return m_Pool;
			}
		}
		private void Awake()
		{
			pool.Initialize();
			m_History = new List<Message>(100);
			m_InputField.EVENT_Submit += OnSubmit;
			m_Interact.Locked += OnLock;
			m_Interact.Released += OnUnlock;
		}

		private void Start()
		{
			LoadHistory();
		}

		private void OnLock()
		{
			m_InputField.src.interactable = false;
		}
		private void OnUnlock()
		{
			m_InputField.src.interactable = true;
		}


		private void OnSubmit(string content)
		{
			if (m_AI == null)
			{
				Debug.LogError("AI is not assigned");
				return;
			}
			if (m_AiMsgPrefab == null)
			{
				Debug.LogError("Message prefab is not assigned");
				return;
			}
			if (m_MyMsgPrefab == null)
			{
				Debug.LogError("My message prefab is not assigned");
				return;
			}

			// Clean up the input field
			m_InputField.Clear();
			m_Interact.AcquireLock(m_AI);

			// prepare chat data, by history
			var chat = new Chat(m_AI.modelName, steam: m_Steam);
			for (int i = 0; i < m_History.Count; i++)
			{
				var msg = m_History[i];
				chat.messages.Add(msg);
			}

			// Spawn my message
			{
				var t0 = pool.Spawn(m_MyMsgPrefab.gameObject, m_SpawnLayer);
				var m0 = t0.GetComponent<UIMessage>();
				var myMsg = new UserMessage(content);
				m_History.Add(myMsg);
				m0.Text = content;
				m0.SetAlignment(eAlign.Right);
			}

			// Spawn AI message
			var token = pool.Spawn(m_AiMsgPrefab.gameObject, m_SpawnLayer);
			var ui = token.GetComponent<UIMessage>();
			ui.Text = "...";
			ui.SetAlignment(eAlign.Left);
			m_ScrollRect.normalizedPosition = Vector2.down;

			chat.AddUserMessage(content);

			chat.AddTool(FunctionCalling.s_FunctionTypes);

			// send to AI
			ChatRequest(chat, ui);
		}

		private async void ChatRequest(Chat chat, UIMessage ui)
		{
			try
			{
				var reply = await m_AI.Chat(chat, (typing) => { _OnMsgStreaming(typing, ui); });
				_OnMsgReply(reply, ui);
			}
			catch (System.Exception ex)
			{
				Debug.LogError(ex);
				_OnMsgStreaming("Error: " + ex.Message, ui);
				m_Interact.ReleaseLock(m_AI);
			}
			return;

			void _OnMsgStreaming(string content, UIMessage ui)
			{
				ui.Text = content;
				m_ScrollRect.normalizedPosition = Vector2.down;
			}

			void _OnMsgReply(ChatCompletion reply, UIMessage ui)
			{
				var msg = reply.choices[0].message;
				ui.Text = msg.content;
				m_History.Add(new AssistantMessage(msg.content));
				m_Interact.ReleaseLock(m_AI);
				m_ScrollRect.normalizedPosition = Vector2.down;
				m_InputField.src.Select();
				SaveHistory();
			}
		}


		[ContextMenu("Test IPAddress")]
		public void FromServerFuncCall()
		{
			var ipAddress = Dns.GetHostEntry(Dns.GetHostName())
				.AddressList
				//.Where(addr => addr.ToString().StartsWith("10."));
				.ToArray();

			foreach (var ip in ipAddress)
				Debug.Log(ip);
		}

		#region Save Load
		private string GetSavePath()
		{
			var savePath = Application.persistentDataPath;
			if (!string.IsNullOrEmpty(m_SaveKey))
				savePath += "/" + m_SaveKey;
			savePath += "/chat_history.json";
			return savePath;
		}
		private async void SaveHistory()
		{
			var json = Utils.ToJson(m_History);
			var path = GetSavePath();
			var dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			await File.WriteAllTextAsync(GetSavePath(), json);
		}

		private async void LoadHistory()
		{
			var token = new CancellationToken();
			var path = GetSavePath();
			if (!File.Exists(path))
				return;
			try
			{
				m_Interact.AcquireLock(this);
				var json = await File.ReadAllTextAsync(path, token);
				m_History = Utils.FromJson<List<Message>>(json);
				for (int i = 0; i < m_History.Count; ++i)
				{
					var msg = m_History[i];
					var prefab = msg is AssistantMessage ? m_AiMsgPrefab : m_MyMsgPrefab;
					var t = pool.Spawn(prefab.gameObject, m_SpawnLayer);
					var ui = t.GetComponent<UIMessage>();
					ui.Text = msg.content;
					ui.SetAlignment(msg is AssistantMessage ? eAlign.Left : eAlign.Right);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError(ex);
			}
			finally
			{
				m_Interact.ReleaseLock(this);
			}
		}

		[ContextMenu("Clear Save")]
		public void ClearSave()
		{
			var path = GetSavePath();
			if (File.Exists(path))
				File.Delete(path);
			if (Application.isPlaying)
			{
				pool.DespawnAll();
				m_History.Clear();
			}
		}

		#endregion Save Load
	}
}