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
            public const string Ping = "ping";
			public const string Heartbeat = "HB";
			public const string HeartbeatAck = "HB_ACK";
			public const string RegisterCamera = "REG_CAM";
			public const string SlaveError = "SLAVE_ERROR";
			public const string SlaveWarning = "SLAVE_WARN";
			public const string StartupComplete = "STARTUP_COMPLETE";
			public const string StartupAck = "STARTUP_ACK";
		}

        private bool isConnected => pipeServer?.IsConnected == true;

		[ReadOnly, SerializeField]
        private bool isWinOverlayReady = false;
        
        private NamedPipeServerStream pipeServer;
        private System.Diagnostics.Process winOverlayProcess;
        private Coroutine heartbeatCoroutine;

		private const int BUFFER_SIZE = 1024;
		
		[SerializeField, Min(0)] int maxRetryCount = 3;
		[SerializeField, Min(1f)] float startupTimeout = 10f; // Timeout for startup completion

        [SerializeField] bool m_ForceRestart = false;
        
        private int consecutiveHeartbeatFailures = 0;
        private float lastHeartbeatTime = 0f;
        private const float HEARTBEAT_TIMEOUT = 10f; // 10 seconds timeout for heartbeat response

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

		private void RestartServer()
		{
			Debug.Log("Restarting server...");
			consecutiveHeartbeatFailures = 0;
			isWinOverlayReady = false;
			StopServer();
			StartServer();
		}
		private void StopServer()
		{
			isWinOverlayReady = false;

			if (heartbeatCoroutine != null)
			{
				StopCoroutine(heartbeatCoroutine);
				heartbeatCoroutine = null;
			}

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
				consecutiveHeartbeatFailures = 0;
				Debug.Log("WinOverlay connected, waiting for startup complete signal...");

                _ = Task.Run(ListenForMessages);

                await Task.Delay(100); // Small delay to ensure listener is ready
				SendMessage(CMD.Ping);

				// Wait for startup complete signal with timeout
				await WaitForStartupComplete();
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Connection failed: {e.Message}");
                StopServer();
                Invoke(nameof(RestartServer), 2f); // Retry after 2 seconds
			}
		}

        private async Task WaitForStartupComplete()
        {
            var startTime = Time.time;
            
            while (!isWinOverlayReady && isConnected)
            {
                if (Time.time - startTime > startupTimeout)
                {
                    Debug.LogError($"WinOverlay startup timeout ({startupTimeout}s). Force starting heartbeat...");
                    break;
                }
                
                await Task.Delay(100); // Check every 100ms
            }
            
            // Send startup acknowledgment
            const string startupAck = "{\"action\":\"" + CMD.StartupAck + "\"}";
            SendMessage(startupAck);
                
            // Start heartbeat after WinOverlay is ready
            Debug.Log("WinOverlay ready, starting heartbeat...");
            heartbeatCoroutine = StartCoroutine(SendHeartbeat());
        }

		private IEnumerator SendHeartbeat()
        {
            while (isConnected && isWinOverlayReady)
            {
                lastHeartbeatTime = Time.time;

				const string HEARTBEAT = "{\"action\":\"" + CMD.Heartbeat + "\"}";
		        SendMessage(HEARTBEAT);
                
                // Wait for heartbeat interval
                yield return new WaitForSeconds(2f);
                
                // Check if we received a response within timeout
                if (Time.time - lastHeartbeatTime > HEARTBEAT_TIMEOUT)
                {
                    consecutiveHeartbeatFailures++;
                    Debug.LogWarning($"Heartbeat timeout! Consecutive failures: {consecutiveHeartbeatFailures}");
                    
                    if (consecutiveHeartbeatFailures >= maxRetryCount)
                    {
                        Debug.LogError($"Max heartbeat failures ({maxRetryCount}) reached. Restarting server...");
                        RestartServer();
                        yield break;
                    }
                }
            }
        }
        
		private async void ListenForMessages()
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            while (pipeServer?.IsConnected == true)
            {
                try
                {
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
						string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        lock (m_Messages)
                        {
                            m_Messages.Enqueue(message);
                        }
                    }
                }
                catch
                {
                    break;
                }
            }
            isWinOverlayReady = false;
            Debug.LogWarning("Lost connection to WinOverlay.");
        }

        private Queue<string> m_Messages = new Queue<string>();
        private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
        private void ProcessMessage(string message)
        {
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
                case CMD.StartupComplete:
                    isWinOverlayReady = true;
                    Debug.Log("WinOverlay startup complete signal received");
                    break;
                case CMD.HeartbeatAck:
                    // Reset failure counter on successful heartbeat response
                    consecutiveHeartbeatFailures = 0;
                    // Debug.Log($"Received {message}");
                    break;
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
        
        void OnDestroy()
        {
            if (heartbeatCoroutine != null)
                StopCoroutine(heartbeatCoroutine);
                
            pipeServer?.Dispose();
            winOverlayProcess?.Kill();
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