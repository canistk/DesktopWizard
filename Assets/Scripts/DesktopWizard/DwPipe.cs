
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace DesktopWizard
{
    public class DwPipe : MonoBehaviour
    {
		[SerializeField] string m_Prefix = "MyPipe";

		private NamedPipeServerStream pipeServer;
		private NamedPipeClientStream pipeClient;
		private CancellationTokenSource m_CancelSrc = null;
		public bool isConnected => pipeServer?.IsConnected == true;
		private bool HandShaked
		{
			get => pipeServer?.IsConnected == true && pipeClient?.IsConnected == true;
		}
		private const int BUFFER_SIZE = 1024;
		private byte[] m_Buffer = new byte[BUFFER_SIZE];
		private List<string> m_CacheIPC = new List<string>(4);
		private readonly object s_CacheIPSLock = new object();

		private void OnEnable()
		{
			RenewToken();
			Task.Run(StartServerAsync);
		}
		private void OnDisable()
		{
			m_CancelSrc.Cancel();
		}

		private void Update()
		{
			HandleCachedMessage();

			lock (s_ExceptionLock)
			{
				UnPackMutilThreadLogs();
			}
		}

		private void RenewToken()
		{
			if (m_CancelSrc != null)
			{
				m_CancelSrc.Cancel();
				m_CancelSrc.Dispose();
			}
			m_CancelSrc = new CancellationTokenSource();
		}

		private async void StartServerAsync()
		{
			while (!m_CancelSrc.IsCancellationRequested)
			{
				await Task.Delay(100);
			}

			var serverName = $"{m_Prefix}_server";
			var clientName = $"{m_Prefix}_client";

			pipeServer?.Dispose();

			pipeServer = new NamedPipeServerStream(serverName, PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			pipeClient = new NamedPipeClientStream(".", clientName, PipeDirection.In, PipeOptions.Asynchronous);
			var job0 = pipeServer.WaitForConnectionAsync(m_CancelSrc.Token);
			var job1 = pipeClient.ConnectAsync(m_CancelSrc.Token);
			await Task.WhenAll(job0, job1);
			tLog("Pipe Server Connected.");
			_ = Task.Run(ListenForMessages);
		}

		private async void ListenForMessages()
		{
			int bytesRead = -1;
			while (HandShaked)
			{
				try
				{
					bytesRead = await pipeClient.ReadAsync(m_Buffer, 0, m_Buffer.Length);
					if (bytesRead > 0)
					{
						var obj = JObject.Parse(Encoding.UTF8.GetString(m_Buffer, 0, bytesRead));
						lock (m_Messages)
						{
							m_Messages.Enqueue(obj);
						}
					}
				}
				catch (System.Exception ex)
				{
					tLogError(ex, $"Pipe ListenForMessages error: {ex}");
				}
			}
		}

		#region Messages

		private Queue<JObject> m_Messages = new Queue<JObject>();
		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
		private void HandleCachedMessage()
		{
			while (m_Messages.Count > 0)
			{
				lock (m_Messages)
				{
					var message = m_Messages.Dequeue();
					ProcessMessage(message);
				}
			}
		}
		private void ProcessMessage(JObject jobj)
		{
			tLog($"Pipe Message Received: {jobj.ToString()}");
		}
		#endregion Messages

		#region MultiThread

		private List<MTBase> m_MulitThreadLogs = new List<MTBase>();
		private static readonly object s_ExceptionLock = new object();
		private void UnPackMutilThreadLogs()
		{
			if (m_MulitThreadLogs.Count == 0)
				return;
			lock (s_ExceptionLock)
			{
				for (int i = 0; i < m_MulitThreadLogs.Count; ++i)
				{
					var log = m_MulitThreadLogs[i];
					if (log is MTException ex)
					{
						if (!string.IsNullOrEmpty(ex.msg))
							Debug.LogError(ex.msg);
						var x = ex.exception;
						while (x != null)
						{
							Debug.LogError(x.Message);
							x = x.InnerException;
						}
					}
					else if (log is MTLog logMsg)
					{
						Debug.Log(logMsg.msg);
					}
					else if (log is MTWarn warnMsg)
					{
						Debug.LogWarning(warnMsg.msg);
					}
				}
				m_MulitThreadLogs.Clear();
			}
		}
		private void tLogError(System.Exception ex, string msg = null) { lock (s_ExceptionLock) m_MulitThreadLogs.Add(new MTException(msg, ex)); }
		private void tLogWarning(string msg) { lock (s_ExceptionLock) m_MulitThreadLogs.Add(new MTWarn(msg)); }
		private void tLog(string msg) { lock (s_ExceptionLock) m_MulitThreadLogs.Add(new MTLog(msg)); }

		#endregion MultiThread

	}
}