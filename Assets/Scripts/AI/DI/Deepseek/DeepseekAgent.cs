// #define JTOKEN_PARSE
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;
using Deepseek.Functions;
namespace Deepseek
{
	[CreateAssetMenu(fileName = "DeepseekAgent", menuName = "Deepseek/DeepseekAgent")]
	public class DeepseekAgent : ScriptableObject
	{
		[Header("Server setup")]
		[SerializeField] string m_ServerAddress = "https://api.deepseek.com";
		[SerializeField] string m_ApiKey = string.Empty;
		[SerializeField] string m_ModelName = "deepseek-chat";
		public string modelName => m_ModelName;
		[Space]
		[Header("EndPoints (OpenAI-like)")]
		[SerializeField] string ep_Model = "models";
		[SerializeField] string ep_Chat = "chat/completions";
		[Space]
		[Header("Debug")]
		[SerializeField] bool m_ShowLog = true;
		[SerializeField] bool m_ShowDevLog = true;


		private Uri baseUri		=> new Uri(m_ServerAddress);
		const string POST		= "POST";
		const string GET		= "GET";
		const System.StringComparison Ignore = System.StringComparison.OrdinalIgnoreCase;


		private UnityWebRequest PrepareHeader(Uri uri, string method,
			DownloadHandler downloadHandler = null,
			UploadHandler uploadHandler = null)
		{
			var req = new UnityWebRequest(uri, method);
			req.SetRequestHeader("Content-Type", "application/json");
			req.SetRequestHeader("Accept", "application/json");
			if (!string.IsNullOrEmpty(m_ApiKey))
				req.SetRequestHeader("Authorization", $"Bearer {m_ApiKey}");
			req.downloadHandler = downloadHandler ?? new DownloadHandlerBuffer();
			if (uploadHandler != null)
				req.uploadHandler = uploadHandler;
			return req;
		}

		private Uri listModeApi => new Uri(baseUri, ep_Model);
		/// <summary>Fetch models from Deepseek server.
		/// <see cref="https://api-docs.deepseek.com/zh-cn/api/list-models"/>
		/// </summary>
		/// <returns></returns>
		public async Task<ModelList> ListModels()
		{
			LOG($"Fetching models from Deepseek server...\n{listModeApi}");
			using (var req = PrepareHeader(listModeApi, GET))
			{
				var oper = req.SendWebRequest();
				// LOG("Sent Request...");

				var startTime = DateTime.UtcNow;
				var anchor = startTime;
				while (!oper.isDone)
				{
					if (req.result >= UnityWebRequest.Result.ConnectionError)
					{
						LOG_ERROR($"{req.result} - Failed to fetch models from Deepseek server: {req.error}");
						break;
					}
					if ((DateTime.UtcNow - anchor).TotalSeconds > 1)
					{
						if (req.downloadHandler is DownloadHandlerBuffer dlBuff)
						{
							DEV_LOG($"Waiting for response... {req.downloadProgress:P2}\n{dlBuff.text}");
						}
						else
						{
							DEV_LOG($"Waiting for response... {req.downloadProgress:P2}");
						}
						anchor = DateTime.UtcNow;
					}
					await Task.Yield();
				}
				LOG($"Server response in {(DateTime.UtcNow - startTime).TotalSeconds} seconds, bytes[{req.downloadedBytes}]");

				try
				{
					if (req.downloadHandler.isDone)
					{
						var response = req.downloadHandler.text;
						DEV_LOG(response);
						var modelList = Deepseek.Utils.FromJson<ModelList>(response);
						return modelList;
					}
				}
				catch (Exception e)
				{
					LOG_ERROR($"Failed to fetch models from Deepseek server: {e.Message}");
				}
			}
			return default;
		}

		public void ListModels(Action<ModelList> success, Action<Exception> fail = null)
		{
			ListModels().ContinueWith((task) =>
			{
				if (task.IsFaulted)
				{
					fail?.Invoke(task.Exception);
				}
				else
				{
					success?.Invoke(task.Result);
				}
			});
		}

		#region ChatCompletion
		private Uri chatApi => new Uri(baseUri, ep_Chat);
		public delegate void OnChatCompletion (ChatCompletion chatCompletion);
		public delegate void OnSteamingMessage(string accumulateMessage);
		public delegate void OnException(System.Exception ex);

		/// <summary>
		/// Send chat request to Deepseek server.
		/// <see cref="https://api-docs.deepseek.com/zh-cn/api/create-chat-completion"/>
		/// </summary>
		/// <param name="chatData"></param>
		/// <param name="steamingCallback"></param>
		/// <returns></returns>
		/// <exception cref="System.Exception"></exception>
		/// <exception cref="Exception"></exception>
		public async Task<ChatCompletion> Chat(Chat chatData, OnSteamingMessage steamingCallback = null)
		{
			try
			{
				if (chatData == null)
					throw new System.Exception("Chat request is null.");
				if (!chatData.IsValid(Deepseek.Chat.ErrorHandle.Exception))
					throw new System.Exception();
				

				UploadHandler uploadHandler = default;
				{
					string json = Deepseek.Utils.ToJson(chatData);
					LOG($"Prepare ChatData json:\n{json}");
					var bytes = Encoding.UTF8.GetBytes(json);
					uploadHandler = new UploadHandlerRaw(bytes);
				}

				using (var req = PrepareHeader(chatApi, POST))
				{
					req.uploadHandler = uploadHandler;

					var startTime = DateTime.UtcNow;
					var isSteamRequest = chatData.stream.HasValue ? chatData.stream.Value : false;

					ChatCompletion response = default;
					if (isSteamRequest)
					{
						response = await _HandleSteamRequest(req, chatData, steamingCallback);
					}
					else
					{
						response = await _HandlePostRequest(req);
					}
					LOG($"Server response in {(DateTime.UtcNow - startTime).TotalSeconds} seconds, bytes[{req.downloadedBytes}]");
					if (req.result > UnityWebRequest.Result.Success)
						LOG_ERROR($"Request Result:{req.result}\nError:{req.error}\n");
					else if (!string.IsNullOrEmpty(req.error))
						LOG_ERROR($"Error\n{req.error}\n\n");
					return response;
				}
			}
			catch (System.Exception ex)
			{
				throw ex;
			}


			async Task<ChatCompletion> _HandleSteamRequest(UnityWebRequest req, Chat chatData, OnSteamingMessage steamingCallback)
			{
				const float REFRESH_RATE = 0.25f;
				const float LOG_RATE = 3f;
				var oper = req.SendWebRequest();
				var anchor = DateTime.UtcNow;
				var logAnchor = anchor;
				var sb = new StringBuilder(2048);
				ChatCompletion lastValid = default;
				List<ToolCall> toolCalls = new List<ToolCall>();
				var pt = 0;
				while (!oper.isDone)
				{
					if (req.result >= UnityWebRequest.Result.ConnectionError)
						throw new System.Exception($"Code:{req.responseCode}, {req.result} - Failed to chat with Deepseek server: {req.error}");

					if ((DateTime.UtcNow - anchor).TotalSeconds > REFRESH_RATE)
					{
						anchor = DateTime.UtcNow;
						if (req.downloadHandler is not DownloadHandlerBuffer dlBuff)
							throw new System.Exception("Logic error");

						var lastPt = pt;
						_HandleSteamData(ref chatData, ref lastValid, dlBuff.text, ref pt, sb, toolCalls);
						if (toolCalls.Count > 0)
							break; // exit while.

						if (lastPt != pt)
						{
							try
							{
								steamingCallback?.Invoke(sb.ToString());
							}
							catch (System.Exception ex)
							{
								// Ignore callback exception
								LOG_EXCEPTION(ex);
							}
						}
					}
					if ((DateTime.UtcNow - logAnchor).TotalSeconds > LOG_RATE)
					{
						DEV_LOG($"Waiting for response..." +
							$"\nU:{req.uploadedBytes}({req.uploadProgress:P2}) D:{req.downloadedBytes}({req.downloadProgress:P2})" +
							$"\nReplyText:{sb.ToString()}");
						logAnchor = DateTime.UtcNow;
					}
					await Task.Yield();
				}

				// Check web request state completeion.
				if (req.result > UnityWebRequest.Result.Success)
				{
					if (req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text))
					{
						// Server had extra error information.
						Debug.LogError(req.downloadHandler.text);
					}
					throw new System.Exception(req.error);
				}

				_HandleSteamData(ref chatData, ref lastValid, req.downloadHandler.text, ref pt, sb, toolCalls);
				if (toolCalls.Count > 0)
				{
					var shouldReply = await _TryHandleToolsCall(toolCalls);
					sb.Clear();
					return await Chat(chatData, steamingCallback);
				}

				// clean up delta data.
				lastValid.choices[0].delta = null;
				// copy delta content(s) into message
				lastValid.choices[0].message = new AssistantMessage(sb.ToString());

				if (m_ShowDevLog)
				{
					var json = Utils.ToJson(lastValid);
					DEV_LOG(json);
				}

				sb.Clear();
				return lastValid;
			}

			// return true, if server require more data to process.
			// return false, processing one way message / waiting response.
			void _HandleSteamData(ref Chat chatData, ref ChatCompletion lastValid, in string txt, ref int pt, System.Text.StringBuilder sb, List<ToolCall> toolCalls)
			{
				const string OPEN_TAG = "data: ";
				const string DONE = "[DONE]";
				int startIdx = -1, sEndIdx = -1, eol = -1;
				do
				{
					// looking for new content
					startIdx	= txt.IndexOf(OPEN_TAG, pt);
					sEndIdx		= startIdx + OPEN_TAG.Length;
					eol			= (sEndIdx >= 0 && txt.Length > sEndIdx) ? txt.IndexOf('\n', sEndIdx) : -1;
					if (startIdx == -1 || // content not found
						eol == -1)  // end of line not found
					{
						// Early return, wait for more data
						break;
					}

					var span = txt.Substring(sEndIdx, eol - sEndIdx).Trim('\n', ' ');
					if (span.Equals(DONE))
					{
						DEV_LOG($"Steam end 1, {DONE} detected.");
						break;
					}

					try
					{
						DEV_LOG(span);
						var tmp = Utils.FromJson<ChatCompletion>(span);
						if (tmp.choices == null)
						{
							throw new System.Exception("Unexpected case. null choices");
						}
						// Note: only one choice is expected. n=1,
						// perhaps further version will support multiple choices.
						var delta = tmp.choices[0].delta;
						if (delta == null)
						{
							throw new System.Exception("Unexpected case. null delta");
						}
						if (!string.IsNullOrEmpty(delta.content))
						{
							sb.Append(delta.content);
						}
						if (delta.toolCalls != null)
						{
							// var shouldReply = await _TryHandleToolsCall(delta.toolCalls);
							toolCalls.AddRange(delta.toolCalls);
						}

						lastValid = tmp;
					}
					catch (Exception ex)
					{
						// ignore deserialize fail
						LOG_EXCEPTION(ex);
						break;
					}

					// move pointer to end of line.
					pt = eol;
					// Process next
				}
				while (startIdx != -1 && eol != -1);
			}

			async Task<ChatCompletion> _HandlePostRequest(UnityWebRequest req)
			{
				const float LOG_RATE = 3f;
				var anchor		= DateTime.UtcNow;

				var oper		= req.SendWebRequest();
				while (!oper.isDone)
				{
					if (req.result >= UnityWebRequest.Result.ConnectionError)
					{
						throw new System.Exception($"Code:{req.responseCode}, {req.result} - Failed to chat with Deepseek server: {req.error}");
					}
					if ((DateTime.UtcNow - anchor).TotalSeconds > LOG_RATE)
					{
						DEV_LOG($"Waiting for response..." +
							$"\nU:{req.uploadedBytes}({req.uploadProgress:P2}) D:{req.downloadedBytes}({req.downloadProgress:P2})");
						anchor = DateTime.UtcNow;
					}
					await Task.Yield();
				}



				if (req.downloadHandler == null || !req.downloadHandler.isDone)
					throw new Exception("Failed to receive response.");

				var json = req.downloadHandler.text;
				DEV_LOG(json);
				var tmp = Utils.FromJson<ChatCompletion>(json);
				
				if (tmp.choices != null &&
					tmp.choices.Length > 0 &&
					tmp.choices[0].message != null &&
					tmp.choices[0].message is AssistantMessage assistantMessage &&
					assistantMessage.toolCalls != null)
				{
					var shouldReply = await _TryHandleToolsCall(assistantMessage.toolCalls);
					tmp = await Chat(chatData, steamingCallback);
				}
				return tmp;
			}

			// true = Found Function Calling request from LLM
			async Task<bool> _TryHandleToolsCall(IList<ToolCall> toolCalls)
			{
				if (toolCalls == null || toolCalls.Count == 0)
					return false;

				// Function Calling request
				// collect request and response those request one by one with matching call id.
				var assistMsg = new AssistantMessage(string.Empty);
				if (assistMsg.toolCalls == null)
					assistMsg.toolCalls = new List<ToolCall>();
				var toolsResponse = new List<Message>();
				foreach (var toolCall in toolCalls)
				{
					if (string.IsNullOrEmpty(toolCall.type))
						continue;
					if (toolCall.type.Equals("function", Ignore))
					{
						if (toolCall.function == null)
							throw new System.Exception("Unexpected case. null function");

						var reply = await FunctionCalling.TryInvoke(toolCall.id, toolCall.function);
						if (reply != null)
						{
							// reconstruct assistant message toolCalls request.
							assistMsg.toolCalls.Add(toolCall);
							// cache multiple tools callback at one.
							toolsResponse.Add(reply);
							break;
						}
					}
				}

				chatData.messages.Add(assistMsg);
				chatData.messages.AddRange(toolsResponse);
				// Require server process the Function Calling result.
				// _ = Chat(chatData, steamingCallback); // let upper level async handle this.
				return true;
			}
		}

		/// <summary>
		/// Send chat request to Deepseek server.
		/// <see cref="https://api-docs.deepseek.com/zh-cn/api/create-chat-completion"/>
		/// </summary>
		/// <param name="data"></param>
		/// <param name="success"></param>
		/// <param name="fail"></param>
		/// <param name="onSteaming"></param>
		/// <exception cref="System.Exception"></exception>
		public void Chat(Chat data, OnChatCompletion success, OnException fail = null, OnSteamingMessage steamingCallback = null)
		{
			Chat(data, steamingCallback).ContinueWith((task) =>
			{
				if (task.IsFaulted)
				{
					fail?.Invoke(task.Exception);
				}
				else
				{
					success?.Invoke(task.Result);
				}
			});
		}
		#endregion ChatCompletion

		#region Debug Log
		private void LOG(string msg, UnityEngine.Object obj = null)
		{
			if (!m_ShowLog)
				return;
			Debug.Log(msg, null);
		}
		private void DEV_LOG(string msg, UnityEngine.Object obj = null)
		{
			if (!m_ShowDevLog)
				return;
			Debug.Log(msg, null);
		}
		private void LOG_WARNING(string msg, UnityEngine.Object obj = null)
		{
			Debug.LogWarning(msg, null);
		}
		private void LOG_ERROR(string msg, UnityEngine.Object obj = null)
			=> Debug.LogError(msg, null);
		private static void LOG_EXCEPTION(System.Exception ex, UnityEngine.Object obj = null)
			=> Debug.LogException(ex, null);
		#endregion
	}
}