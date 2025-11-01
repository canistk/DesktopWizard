using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
    public class OverlayManager : ApplicationContext
    {
        private Unity3DConnector connector;
        private System.Timers.Timer reconnectTimer;
        private const int RECONNECT_INTERVAL = 3000;

        public OverlayManager()
        {
            InitializeConnector();
            StartReconnectTimer();
        }

        private void InitializeConnector()
        {
            connector = new Unity3DConnector();
            connector.MessageReceived += OnMessageReceived;
            connector.ConnectionLost += OnConnectionLost;
            connector.ConnectionEstablished += OnConnectionEstablished;
            
            _ = Task.Run(async () => await connector.ConnectAsync());
        }

        private void OnMessageReceived(string message)
        {
            // 處理來自 Unity3D 的訊息
            if (message.Contains("HEARTBEAT"))
            {
                // 回應 Unity3D 的心跳
                connector.SendMessage("{\"action\":\"HEARTBEAT_ACK\", \"msg\":\"Hello World\"}");
            }
            else if (message.Contains("UPDATE_CAMERAS"))
            {
                // 處理相機更新訊息
            }
        }

        private void OnConnectionLost()
        {
            // 連接丟失時的處理
        }

        private void OnConnectionEstablished()
        {
            // 連接建立時的處理
        }

        private void StartReconnectTimer()
        {
            reconnectTimer = new System.Timers.Timer(RECONNECT_INTERVAL);
            reconnectTimer.Elapsed += async (s, e) => await TryReconnect();
            reconnectTimer.Start();
        }

        private async Task TryReconnect()
        {
            if (!connector.IsConnected)
            {
                await connector.ConnectAsync();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                reconnectTimer?.Stop();
                reconnectTimer?.Dispose();
                connector?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}