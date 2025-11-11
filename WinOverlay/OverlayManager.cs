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
            public const string SlaveInfo = "SLAVE_INFO";
            public const string RegisterCamera = "REG_CAM";
            public const string UnregisterCamera = "UNREG_CAM";
        }
        private Unity3DConnector u3d => Unity3DConnector.Instance;
        private const StringComparison IGNORE = StringComparison.OrdinalIgnoreCase;
        private Dictionary<string /* prefix */, DwForm> m_ActiveCameras = new Dictionary<string, DwForm>();

        public OverlayManager()
        {
            InitializeConnector();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all active cameras
                foreach (var kvp in m_ActiveCameras)
                {
                    // kvp.Value?.Dispose();
                }
                m_ActiveCameras.Clear();
                
                Unity3DConnector.DisposeInstance();
            }
            base.Dispose(disposing);
        }

        private void InitializeConnector()
        {
			u3d.MessageReceived -= OnMessageReceived;
            u3d.ConnectionLosted -= OnConnectionLost;
			u3d.MessageReceived += OnMessageReceived;
            u3d.ConnectionLosted += OnConnectionLost;

            _ = Task.Run(async () => await u3d.ConnectAsync());
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
                UnregisterCamera(jObj);
                break;
                default:
                SendError("SLAVE: Unknown action received: " + action);
                break;
            }
        }


		private void Log(string message) => System.Diagnostics.Debug.WriteLine(message, "INFO");
		private void Warn(string message) => System.Diagnostics.Debug.WriteLine(message, "WARNING");
		private void Error(string message) => System.Diagnostics.Debug.WriteLine(message, "ERROR");

		private void OnConnectionLost()
        {
            // Exit application when connection is lost
            Error("Connection lost. Exiting OverlayManager.");
            InitializeConnector();
			//Dispose();
			//ExitThread();
		}

        public void SendError(string message)
        {
            using (var err = new MyAction(CMD.SlaveError))
            {
                err.Add("message", message);
                u3d.SendMessage(err);
            }
        }
        public void SendWarning(string message)
        {
            using (var warn = new MyAction(CMD.SlaveWarning))
            {
                warn.Add("message", message);
                u3d.SendMessage(warn);
            }
        }

        public void SendInfo(string message)
        {
            using (var info = new MyAction(CMD.SlaveInfo))
            {
                info.Add("message", message);
                u3d.SendMessage(info);
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
            // Check if camera is already registered
            if (m_ActiveCameras.ContainsKey(prefix))
            {
                SendError($"Camera {cameraId} is already registered");
                return;
            }
            
            try
            {
                // Create DwForm for the camera
                var dwForm = new DwForm(cameraId);
                m_ActiveCameras.Add(prefix, dwForm);
                
                // Show the form
                dwForm.Show();
                dwForm.TopLevel = true;

				SendWarning($"Camera {cameraId} registered successfully");
            }
            catch (Exception ex)
            {
                SendError($"Failed to register camera {cameraId}: {ex.Message}");
            }
        }

        private void UnregisterCamera(JObject jObj)
        {
            if (!jObj.TryGetValue("cameraId", IGNORE,
                out var camIdToken))
            {
                SendError("UnregisterCamera missing cameraId");
                return;
            }
            var cameraId = camIdToken.Value<int>();
            
            UnregisterCamera(cameraId);
        }

        private void UnregisterCamera(int cameraId)
        {
            var prefix = $"DwCamera_{cameraId}";
            var sm1 = $"{prefix}_1";
            var sm2 = $"{prefix}_2";
            
            if (m_ActiveCameras.TryGetValue(prefix, out var dwForm))
            {
                try
                {
                    // Close and dispose the form
                    dwForm.Close();
                    dwForm.Dispose();
                    m_ActiveCameras.Remove(prefix);
                    
                    SendWarning($"Camera {cameraId} unregistered successfully");
                }
                catch (Exception ex)
                {
                    SendError($"Failed to unregister camera {cameraId}: {ex.Message}");
                }
            }
            else
            {
                SendWarning($"Camera {cameraId} was not registered");
            }
        }
    }
}