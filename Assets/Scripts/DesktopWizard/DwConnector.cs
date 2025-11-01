using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Process = System.Diagnostics.Process;
namespace DesktopWizard
{
    public class DwConnector : MonoBehaviour
    {
        private NamedPipeServerStream pipeServer;
        private System.Diagnostics.Process winOverlayProcess;
        private bool isConnected = false;
        private Coroutine heartbeatCoroutine;
        
        void Start()
        {
            StartServer();
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
                
                StartWinOverlay();
                await WaitForConnection();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start server: {e.Message}");
            }
        }
        
        private void StartWinOverlay()
        {
            try
            {
                string exePath = Path.Combine(Application.streamingAssetsPath, "WinOverlay", "WinOverlay.exe");
                winOverlayProcess = Process.Start(exePath, "-test");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start WinOverlay: {e.Message}");
            }
        }
        
        private async Task WaitForConnection()
        {
            try
            {
                await pipeServer.WaitForConnectionAsync();
                isConnected = true;
                Debug.Log("WinOverlay connected");
                
                heartbeatCoroutine = StartCoroutine(SendHeartbeat());
                _ = Task.Run(ListenForMessages);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Connection failed: {e.Message}");
            }
        }
        
        private IEnumerator SendHeartbeat()
        {
            while (isConnected)
            {
                SendMessage("{\"action\":\"HEARTBEAT\"}");
                yield return new WaitForSeconds(2f);
            }
        }
        
        private async void ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            
            while (pipeServer?.IsConnected == true)
            {
                try
                {
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessMessage(message);
                    }
                }
                catch
                {
                    isConnected = false;
                    break;
                }
            }
        }
        
        private void ProcessMessage(string message)
        {
            if (message.Contains("HEARTBEAT_ACK"))
            {
                // WinOverlay 回應心跳
                Debug.Log($"Received {message}");
			}
        }
        
        public void SendMessage(string message)
        {
            if (pipeServer?.IsConnected == true)
            {
                try
                {
                    var data = Encoding.UTF8.GetBytes(message);
                    pipeServer.Write(data, 0, data.Length);
                    pipeServer.Flush();
                }
                catch
                {
                    isConnected = false;
                }
            }
        }
        
        void OnDestroy()
        {
            if (heartbeatCoroutine != null)
                StopCoroutine(heartbeatCoroutine);
                
            pipeServer?.Dispose();
            winOverlayProcess?.Kill();
        }
    }
}