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
            public const string Ping = "Ping";
            public const string Pong = "Pong";
            public const string SlaveError = "SLAVE_ERROR";
            public const string SlaveWarning = "SLAVE_WARN";
            public const string RegisterCamera = "REG_CAM";
            public const string UnregisterCamera = "UNREG_CAM";
        }
        private Unity3DConnector connector;
        private const StringComparison IGNORE = StringComparison.OrdinalIgnoreCase;

        public OverlayManager()
        {
            InitializeConnector();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                connector?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeConnector()
        {
            connector = new Unity3DConnector();
            connector.MessageReceived += OnMessageReceived;
            connector.ConnectionLosted += OnConnectionLost;

            _ = Task.Run(async () => await connector.ConnectAsync());
        }

        private void OnMessageReceived(string message)
        {
            JObject jObj = JObject.Parse(message);
            if (!jObj.TryGetValue(CMD.Action, IGNORE,
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
                case CMD.UnregisterCamera:
                default:
                SendError("SLAVE: Unknown action received: " + action);
                break;
            }
        }
        private void OnConnectionLost()
        {
            // Exit application when connection is lost
            Dispose();
            ExitThread();
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
			// TODO: Create DwForm here, and pass shared memory reference.
		}

		private void UnregisterCamera(int cameraId)
        {
            var prefix = $"DwCamera_{cameraId}";
            var sm1 = $"{prefix}_1";
            var sm2 = $"{prefix}_2";
			// TODO: Dispose DwForm here.
		}
	}
}