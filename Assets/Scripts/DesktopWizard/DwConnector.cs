using Kit2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlasticPipe.PlasticProtocol.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
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
			public const string RegisterCamera = "REG_CAM";
			public const string SlaveError = "SLAVE_ERROR";
			public const string SlaveWarning = "SLAVE_WARN";
		}

        private bool isConnected => pipeServer?.IsConnected == true;

		
        
        private NamedPipeServerStream pipeServer;
        private System.Diagnostics.Process winOverlayProcess;

		[ReadOnly, SerializeField]
		bool m_HandShaked = false;
		private const int BUFFER_SIZE = 1024;
		private byte[] m_Buffer = new byte[BUFFER_SIZE];

		[SerializeField, Min(0)] int maxRetryCount = 3;
		[SerializeField, Min(1f)] float startupTimeout = 10f; // Timeout for startup completion
		[SerializeField] bool m_ForceRestart = false;

		private Queue<string> m_Messages = new Queue<string>();
		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
		void OnEnable()
        {
            StartServer();
        }
        void OnDisable()
        {
            StopServer();
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
		}
		void OnDestroy()
		{
			StopServer();
		}


		private void RestartServer()
		{
			Debug.Log("Restarting server...");
			StopServer();
			StartServer();
		}
		private void StopServer()
		{
            m_HandShaked = false;
			pipeServer?.Dispose();
			pipeServer = null;

			if (winOverlayProcess != null && !winOverlayProcess.HasExited)
			{
				winOverlayProcess.Kill();
				winOverlayProcess = null;
			}
		}

		private async void StartServer()
        {
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
                Debug.LogError($"Failed to start server: {e.Message}");
            }

			try
			{
				string exePath = Path.Combine(Application.streamingAssetsPath, "WinOverlay", "WinOverlay.exe");
				winOverlayProcess = Process.Start(exePath, "-test");
				Debug.Log("WinOverlay process started, waiting for startup completion...");
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Failed to start WinOverlay: {e.Message}");
			}

			try
			{
				await pipeServer.WaitForConnectionAsync();
                _ = Task.Run(ListenForMessages);
				Debug.Log("WinOverlay connected, waiting for startup complete signal...");


                // Wait for handshake
                m_HandShaked = false;
				int retryCount = 0;
				var startTime = Time.time;
                do
                {
                    await Task.Delay(500);
                    SendMessage(CMD.Ping);
                }
                while (!m_HandShaked && retryCount++ < 10);

                if (m_HandShaked)
                    Debug.Log($"Connection established, retry = {retryCount}.");
                else
                    Debug.LogWarning("Handshake failed, but continuing...");
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Connection failed: {e.Message}");
                StopServer();
                Invoke(nameof(RestartServer), 2f); // Retry after 2 seconds
			}
		}

		private async void ListenForMessages()
        {
            while (pipeServer?.IsConnected == true)
            {
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
                    Debug.LogError($"Error: {ex.Message}");
					break;
                }
            }

			// Reconnection logic + restart WinOverlay
			if (this.isActiveAndEnabled)
            {
                Debug.LogWarning("Lost connection to WinOverlay.");
                RestartServer();
            }
		}

        private void ProcessMessage(string message)
        {
            // System level
            switch (message)
            {
                case CMD.Ping:
                SendMessage(CMD.Pong);
                return;
                case CMD.Pong:
                m_HandShaked = true;
                Debug.Log("Received Pong from WinOverlay.");
                return;
            }

			// Application level
			var jObj = JObject.Parse(message);
            if (!jObj.TryGetValue(CMD.Action, IGNORE,
                out var aToken))
            {
                Debug.LogWarning("Received message without action: \n"+ message);
                return;
			}
            var action = aToken.Value<string>();

            switch(action)
            {
                case CMD.SlaveWarning:
                    Debug.LogWarning(message);
                    break;
                case CMD.SlaveError:
                    Debug.LogError(message);
                    break;
				default:
                    Debug.LogError($"Received unknown action: {action}");
                    break;
			}
        }
        
        public void SendMessage(string message)
        {
            if (!isConnected)
                return;
            try
            {
                var data = Encoding.UTF8.GetBytes(message);
                pipeServer.Write(data, 0, data.Length);
                pipeServer.Flush();
            }
            catch (Exception ex)
			{
                Debug.LogWarning("Failed to send message: " + message);
            }
        }
        
		public void Register(DwCamera camera)
        {
            // Only register camera if WinOverlay is ready

            var obj = new Dictionary<string, object>();
            obj.Add("action", CMD.RegisterCamera);
            obj.Add("cameraId", camera.id);

			// GPU worker sharememrory name
			// var sm_namePrefix = $"DwCamera_{camera.id}";
			//string registerMessage = $"{{\"action\":\"REGISTER\",\"cameraId\":\"{camera.CameraId}\"}}";
			string registerMessage = JsonUtility.ToJson(obj);
            SendMessage(registerMessage);
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