using Kit2;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Process = System.Diagnostics.Process;
namespace DesktopWizard
{
    public class DwConnector : MonoBehaviour
    {
        public static class CMD
        {
            public const string Action = "action";
            public const string Ping = "Ping";
            public const string Pong = "Pong";
			public const string SlaveError = "SLAVE_ERROR";
			public const string SlaveWarning = "SLAVE_WARN";
            public const string SlaveInfo = "SLAVE_INFO";
			public const string RegisterCamera = "REG_CAM";
            public const string UnregisterCamera = "UNREG_CAM";
        }

        #region Singleton
        private static bool s_AppQuit = false;
        private static DwConnector s_Instance = null;
        public static DwConnector Instance
        {
            get
            {
                if (!s_AppQuit && s_Instance == null)
                {
                    var go = new GameObject("OSProxy");
                    go.AddComponent<DwConnector>();
                    DontDestroyOnLoad(go);
                }
                return s_Instance;
            }
        }
        #endregion Singleton

        private NamedPipeServerStream pipeServer;
        private NamedPipeClientStream pipeClient;
		private CancellationTokenSource m_CancelSrc = null;
		private bool isConnected => pipeServer?.IsConnected == true;
        private System.Diagnostics.Process winOverlayProcess;

        private bool HandShaked
        {
            get => pipeServer?.IsConnected == true && pipeClient?.IsConnected == true;
        }
        private const int BUFFER_SIZE = 1024;
        private byte[] m_Buffer = new byte[BUFFER_SIZE];
		private List<string> m_CacheIPC = new List<string>(4);
		private readonly object s_CacheIPSLock = new object();

		[SerializeField, Min(0)] int m_MaxRetryCount = 3;
        [SerializeField] bool m_ForceRestart = false;

        private Queue<string> m_Messages = new Queue<string>();
        private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;

		#region System

		private void Awake()
		{
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(this.gameObject);
                return;
			}
			s_Instance = this;
		}
		void OnEnable()
        {
            StartServer();
        }
        void OnDisable()
        {
            StopServer();
			m_CacheIPC.Clear();
		}

        private void Update()
        {
            if (m_ForceRestart)
            {
                m_ForceRestart = false;
                RestartServer();
            }

            while (m_Messages.Count > 0)
            {
                lock (m_Messages)
                {
                    var message = m_Messages.Dequeue();
                    ProcessMessage(message);
                }
            }

            lock (s_ExceptionLock)
            {
                UnPackMutilThreadLogs();
            }
		}
        void OnDestroy()
        {
            s_AppQuit = true;
            StopServer();
        }
        #endregion System

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
        private void tLogError(Exception ex, string msg = null) { lock (s_ExceptionLock) m_MulitThreadLogs.Add(new MTException(msg, ex)); }
        private void tLogWarning(string msg) { lock (s_ExceptionLock) m_MulitThreadLogs.Add(new MTWarn(msg)); }
        private void tLog(string msg) { lock (s_ExceptionLock) m_MulitThreadLogs.Add(new MTLog(msg)); }

		#endregion MultiThread

		private void RestartServer()
		{
			if (s_AppQuit)
				return;
			Debug.Log("Restarting server...");
			StopServer();
			StartServer();
		}
		private void StopServer()
		{
            m_CancelSrc?.Cancel();
            m_CancelSrc?.Dispose();
            m_CancelSrc = null;
            pipeServer?.Close();
			pipeServer?.Dispose();
			pipeServer = null;
            pipeClient?.Close();
			pipeClient?.Dispose();
            pipeClient = null;

			if (winOverlayProcess != null && !winOverlayProcess.HasExited)
			{
				winOverlayProcess.Kill();
                winOverlayProcess.Dispose();
				winOverlayProcess = null;
			}
		}

		private async void StartServer()
        {
            if (s_AppQuit)
                return;
            int step = 0;
            CancellationToken token;
			try
            {
                if (m_CancelSrc == null)
                {
                    m_CancelSrc = new CancellationTokenSource();
                }
                else
                {
                    m_CancelSrc.Cancel();
                    m_CancelSrc.Dispose();
                    m_CancelSrc = new CancellationTokenSource();
				}
                token = m_CancelSrc.Token;
				pipeServer?.Dispose();
				pipeServer = new NamedPipeServerStream(
                    "Unity3DServer",
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );
                pipeClient?.Dispose();
				pipeClient = new NamedPipeClientStream(
                    ".",
                    "WinOverlay",
                    PipeDirection.In,
                    PipeOptions.Asynchronous
                );

                tLog($"Dw[{++step}]: Starting server...");
                var job0 = pipeServer.WaitForConnectionAsync(token);
                var job1 = pipeClient.ConnectAsync(token);
                await Task.Delay(10, token); // Slight delay to ensure pipes are ready

				// Check if WinOverlay is already running
				if (job1.IsCompletedSuccessfully)
				{
					tLog($"Dw[{++step}]: WinOverlay is already running.");
				}
				else
				{
					// assume WinOverlay is not running, start it
					// Start WinOverlay process
					string exePath = Path.Combine(Application.streamingAssetsPath, "WinOverlay", "WinOverlay.exe");
					winOverlayProcess = Process.Start(exePath, "-test");
				    tLog($"Dw[{++step}]: WinOverlay process started, waiting for startup completion...");
				}
                await Task.WhenAll(job0, job1);

                tLog($"Dw[{++step}]: WinOverlay connected.");
				_ = Task.Run(ListenForMessages);

                if (!HandShaked)
                    throw new Exception($"Dw[{++step}]: Handshake failed.");
                tLog($"Dw[{++step}]: WinOverlay Handshake succeeded.");

                // Process cached messages
                FlushCachedIPS();
			}
            catch (System.Exception ex)
            {
                tLogError(ex, $"Failed to start server: {ex.Message}");
                StopServer();
                Invoke(nameof(RestartServer), 2f); // Retry after 2 seconds
                return;
            }
        }

        private async void ListenForMessages()
        {
            CancellationToken token;
            try
            {
                token = m_CancelSrc?.Token ?? default;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            while (pipeClient?.IsConnected == true && !token.IsCancellationRequested)
            {
                if (s_AppQuit)
                    return;
				try
                {
                    int bytesRead = await pipeClient.ReadAsync(m_Buffer, 0, m_Buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(m_Buffer, 0, bytesRead);

                        lock (m_Messages)
                        {
                            m_Messages.Enqueue(message);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    tLogError(ex, $"Receive message failed:");
                    break;
                }
            }

            // Reconnection logic + restart WinOverlay
            if (!token.IsCancellationRequested &&
                this.isActiveAndEnabled &&
                !s_AppQuit)
            {
                tLogWarning("Lost connection to WinOverlay.");
                RestartServer();
            }
        }

        private void ProcessMessage(string message)
        {
            // Application level
            var jObj = JObject.Parse(message);
            if (!jObj.TryGetValue(CMD.Action, IGNORE,
                out var aToken))
            {
                Debug.LogWarning("Received message without action: \n" + message);
                return;
            }
            var action = aToken.Value<string>();

            switch (action)
            {
                case CMD.SlaveInfo:
                {
                    var msg = jObj.GetValue("message", IGNORE);
                    Debug.Log($"SLAVE:" + msg);
				}
                break;
				case CMD.SlaveWarning:
                {
                    var msg = jObj.GetValue("message", IGNORE);
                    Debug.LogWarning($"SLAVE:" + msg);
                }
                break;
                case CMD.SlaveError:
                {
                    var msg = jObj.GetValue("message", IGNORE);
                    Debug.LogError($"SLAVE:" + msg);
                }
                break;
                default:
                Debug.LogError($"Received unknown action: {action}");
                break;
            }
        }

		private readonly SemaphoreSlim m_SendSemaphore = new SemaphoreSlim(1, 1);
		public void SendMessage(MTAction action) => IPC(action.ToJson());
		public void IPC(string message)
        {
            if (s_AppQuit)
                return;
			if (!isConnected)
            {
                lock (s_CacheIPSLock)
                {
					m_CacheIPC.Add(message);
                }
				return;
            }
            Task.Run(async () =>
            {
                await m_SendSemaphore.WaitAsync();
                try
                {
                    var data = Encoding.UTF8.GetBytes(message);
                    pipeServer.Write(data, 0, data.Length);
                    pipeServer.Flush();
                }
                catch (Exception ex)
                {
					tLogError(ex, "Failed to send message: " + message);
				}
				finally
                {
                    m_SendSemaphore.Release();
				}
            });
        }
        private void FlushCachedIPS()
        {
            lock (s_CacheIPSLock)
            {
                for (int i = 0; i < m_CacheIPC.Count; i++)
                {
                    IPC(m_CacheIPC[i]);
                }
                m_CacheIPC.Clear();
            }
		}
        public void Register(DwCamera camera)
        {
            using (var data = new MTAction(CMD.RegisterCamera))
            {
                data.Add("cameraId", camera.id);
                SendMessage(data);
            }
        }

        public void Unregister(DwCamera camera)
        {
            using (var data = new MTAction(CMD.UnregisterCamera))
            {
                data.Add("cameraId", camera.id);
                SendMessage(data);
            }
		}
    }

}