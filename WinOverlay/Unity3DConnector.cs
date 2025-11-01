using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace WinOverlay
{
    public class Unity3DConnector : IDisposable
    {
        private NamedPipeClientStream pipeClient;
        private System.Timers.Timer heartbeatTimer;
        private DateTime lastHeartbeat = DateTime.Now;
        private bool isConnected = false;
        
        public event Action<string> MessageReceived;
        public event Action ConnectionLost;
        public event Action ConnectionEstablished;

        public bool IsConnected => isConnected && pipeClient?.IsConnected == true;

        public async Task<bool> ConnectAsync()
        {
            try
            {
                pipeClient?.Dispose();
                pipeClient = new NamedPipeClientStream(".", "DwCamera_Control", PipeDirection.InOut);
                
                await pipeClient.ConnectAsync(5000);
                isConnected = true;
                lastHeartbeat = DateTime.Now;
                
                StartHeartbeat();
                _ = Task.Run(ListenForMessages);
                
                ConnectionEstablished?.Invoke();
                return true;
            }
            catch
            {
                isConnected = false;
                return false;
            }
        }

        private void StartHeartbeat()
        {
            heartbeatTimer?.Stop();
            heartbeatTimer = new System.Timers.Timer(1000); // 每秒檢查一次
            heartbeatTimer.Elapsed += (s, e) => CheckHeartbeatTimeout();
            heartbeatTimer.Start();
        }

        private void CheckHeartbeatTimeout()
        {
            // 檢查心跳超時 - WinOverlay 被動接收 Unity3D 的心跳
            if ((DateTime.Now - lastHeartbeat).TotalSeconds > 10)
            {
                isConnected = false;
                ConnectionLost?.Invoke();
            }
        }

        private async void ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            
            while (pipeClient?.IsConnected == true)
            {
                try
                {
                    int bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        lastHeartbeat = DateTime.Now;
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(message);
                    }
                }
                catch
                {
                    isConnected = false;
                    ConnectionLost?.Invoke();
                    break;
                }
            }
        }

        public void SendMessage(string message)
        {
            if (IsConnected)
            {
                try
                {
                    var data = Encoding.UTF8.GetBytes(message);
                    pipeClient.Write(data, 0, data.Length);
                    pipeClient.Flush();
                }
                catch
                {
                    isConnected = false;
                    ConnectionLost?.Invoke();
                }
            }
        }

        public void Dispose()
        {
            heartbeatTimer?.Stop();
            heartbeatTimer?.Dispose();
            pipeClient?.Dispose();
        }
    }
}