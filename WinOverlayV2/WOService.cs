using System;
using System.Collections.Generic;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using Share;
namespace WinOverlay
{
	/// <summary>
	/// Provides the main service functionality for managing camera overlays.
	/// Depends on WOMessagePipe for communication with Unity.
	/// </summary>
	public class WOService : IDisposable
    {
        private WOMessagePipe u3d => WOMessagePipe.Instance;
        private const StringComparison IGNORE = StringComparison.OrdinalIgnoreCase;
        private Dictionary<string /* prefix */, WOForm> m_ActiveCameras = new Dictionary<string, WOForm>();

        public WOService()
        {
            InitializeConnector();
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                ClearUpOverlay();
                u3d.MessageReceived -= OnMessageReceived;
                u3d.ConnectionLosted -= OnConnectionLost;
                u3d.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void InitializeConnector()
        {
			u3d.MessageReceived -= OnMessageReceived;
            u3d.ConnectionLosted -= OnConnectionLost;
			u3d.MessageReceived += OnMessageReceived;
            u3d.ConnectionLosted += OnConnectionLost;
            u3d.Connect();
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

		private void OnConnectionLost()
        {
            // Exit application when connection is lost
            Debug.Error("Connection lost. OverlayManager shutting down.");
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Debug.Warn("Debugger is attached. Not exiting application.");
                // Clean up existing state
                u3d.MessageReceived -= OnMessageReceived;
                u3d.ConnectionLosted -= OnConnectionLost;
                u3d.Dispose();

                // Re-initialize connector for debugging
                InitializeConnector();
            }
            else
            {
				// close application - WPF version
				System.Windows.Application.Current?.Shutdown();
            }
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
                var dwForm = new WOForm(cameraId);
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

        private void ClearUpOverlay()
        {
			// Dispose all active cameras
			foreach (var kvp in m_ActiveCameras)
			{
				kvp.Value?.Dispose();
			}
			m_ActiveCameras.Clear();
		}
    }
}