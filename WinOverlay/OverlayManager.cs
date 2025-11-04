using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace WinOverlay
{

    public class OverlayManager : ApplicationContext
    {
		public static class CMD
		{
            public const string Action = "action";
			public const string RegisterCamera = "REG_CAM";
            public const string SlaveError =  "SLAVE_ERROR";
            public const string SlaveWarning = "SLAVE_WARN";
		}
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
            connector.ConnectionEstablished += OnConnectionEstablished;
            
            _ = Task.Run(async () => await connector.ConnectAsync());

        }

        private const string ACTION = "action";
        private const StringComparison IGNORE = StringComparison.OrdinalIgnoreCase;
        private void OnMessageReceived(string message)
        {
			JObject jObj = JObject.Parse(message);
            if (!jObj.TryGetValue(ACTION, IGNORE,
                out var aToken))
            {
                return;
            }
			var action = aToken.Value<string>();
            switch (action)
            {
                case CMD.RegisterCamera:
				    RegisterCamera(jObj);
				    break;
				default:
                    SendError("SLAVE: Unknown action received: " + action);
                    break;
			}
        }

        private void RegisterCamera(JObject jObj)
        {
            if (!jObj.TryGetValue("cameraId", IGNORE,
                out var camIdToken))
            {
                SendError("RegisterCamera missing cameraId");
                return;
			}
            var cameraId = camIdToken.Value<int>();

            var prefix = $"DwCamera_{cameraId}";
            var sm1 = $"{prefix}_1";
            var sm2 = $"{prefix}_2";
		}

		public void SendError(string message)
        {
            using (var err = new MyAction(CMD.SlaveError))
            {
                err.Add("message", message);
                connector.SendMessage(err);
            }
		}
		public void SendWarning(string message)
        {
            using (var warn = new MyAction(CMD.SlaveWarning))
            {
                warn.Add("message", message);
                connector.SendMessage(warn);
            }
        }

        private async void OnConnectionEstablished()
        {
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