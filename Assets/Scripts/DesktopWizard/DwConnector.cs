using Kit2;
using Newtonsoft.Json;
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
        private bool isConnected => pipeServer?.IsConnected == true;
        private System.Diagnostics.Process winOverlayProcess;

        [ReadOnly, SerializeField]
        bool m_HandShaked0 = false; // client to server
        bool m_HandShaked1 = false; // server to client
        private bool HandShaked
        {
            get => m_HandShaked0 && m_HandShaked1;
        }
        private const int BUFFER_SIZE = 1024;
        private byte[] m_Buffer = new byte[BUFFER_SIZE];
		private List<string> m_CacheIPC = new List<string>(4);

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
			m_CacheIPC?.Clear();
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
            m_HandShaked0 = m_HandShaked1 = false;
            pipeServer?.Dispose();
            pipeServer = null;

			if (winOverlayProcess != null && !winOverlayProcess.HasExited)
            {
                winOverlayProcess.Kill();
                winOverlayProcess = null;
            }
        }

		#region MultiThread
		private abstract class MTBase { }
        private class MTException : MTBase
        {
            public Exception exception;
            public string msg;
			public MTException(string msg, Exception ex)
            {
                this.msg = msg;
                this.exception = ex;
			}
		}
        private class MTLog : MTBase
        {
            public string msg;
            public MTLog(string msg)  => this.msg = msg;
        }

        private class MTWarn : MTBase
        {
            public string msg;
            public MTWarn(string msg) => this.msg = msg;
		}

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

		private async void StartServer()
        {
            if (s_AppQuit)
                return;
            try
            {
                pipeServer = new NamedPipeServerStream(
                    "DwCamera_Control",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );
            }
            catch (System.Exception e)
            {
                tLogError(e, $"Failed to start server: {e.Message}");
            }

            try
            {
                string exePath = Path.Combine(Application.streamingAssetsPath, "WinOverlay", "WinOverlay.exe");
                winOverlayProcess = Process.Start(exePath, "-test");
                Debug.Log("WinOverlay process started, waiting for startup completion...");
            }
            catch (System.Exception e)
            {
				tLogError(e, $"Failed to start WinOverlay: {e.Message}");
            }

            try
            {
                m_HandShaked0 = m_HandShaked1 = false;
                await pipeServer.WaitForConnectionAsync();
                _ = Task.Run(ListenForMessages);
                tLog("WinOverlay connected, waiting for startup complete signal...");


                // Wait for handshake
                int retryCount = 0;
                var startTime = Time.time;
                do
                {
                    await Task.Delay(500);
                    if (!m_HandShaked0)
                        IPC(CMD.Ping);
                }
                while (!HandShaked && retryCount++ <= m_MaxRetryCount);

                if (!HandShaked)
                    throw new Exception("Handshake failed.");

                tLog($"Connection established, retry = {retryCount - 1}.");

				// Process cached messages
				for (int i = 0; i < m_CacheIPC.Count; i++)
                {
                    IPC(m_CacheIPC[i]);
				}
                m_CacheIPC.Clear();
			}
            catch (System.Exception ex)
            {
                tLogError(ex, $"Connection failed:");
                StopServer();
                Invoke(nameof(RestartServer), 2f); // Retry after 2 seconds
            }
        }

        private async void ListenForMessages()
        {
            while (pipeServer?.IsConnected == true)
            {
                if (s_AppQuit)
                    return;
				try
                {
                    int bytesRead = await pipeServer.ReadAsync(m_Buffer, 0, m_Buffer.Length);
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
            if (this.isActiveAndEnabled && !s_AppQuit)
            {
                tLogWarning("Lost connection to WinOverlay.");
                RestartServer();
            }
        }

        private void ProcessMessage(string message)
        {
            // System level
            switch (message)
            {
                // Ping-pong handshake
                case CMD.Ping: // client to server
                m_HandShaked0 = true;
                IPC(CMD.Pong);
                Debug.Log($"Received WinOverlay {(m_HandShaked0 ? 'T' : 'F')}{(m_HandShaked1 ? 'T' : 'F')}");
                return;
                case CMD.Pong: // server to client
                m_HandShaked1 = true;
                Debug.Log($"Received WinOverlay {(m_HandShaked0 ? 'T' : 'F')}{(m_HandShaked1 ? 'T' : 'F')}");
                return;
            }

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

		private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
		public void SendMessage(MyAction action) => IPC(action.ToJson());
		public void IPC(string message)
        {
            if (s_AppQuit)
                return;
			if (!isConnected)
            {
				m_CacheIPC.Add(message);
				return;
            }
            Task.Run(async () =>
            {
                await _sendSemaphore.WaitAsync();
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
                    _sendSemaphore.Release();
				}
            });
        }

        public void Register(DwCamera camera)
        {
            using (var data = new MyAction(CMD.RegisterCamera))
            {
                data.Add("cameraId", camera.id);
                SendMessage(data);
            }
        }

        public void Unregister(DwCamera camera)
        {
            using (var data = new MyAction(CMD.UnregisterCamera))
            {
                data.Add("cameraId", camera.id);
                SendMessage(data);
            }
		}
    }

	public class MyAction : Dictionary<string, object>, IDisposable
	{
		public MyAction(string value)
		{
			this.Add("action", value);
		}
		public override string ToString() => ToJson();
		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}

		public void Dispose()
		{
			this.Clear();
		}
	}
}