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
	public class WoService : IDisposable
    {
        private static WoMessagePipe u3d => WoMessagePipe.Instance;
        private const StringComparison IGNORE = StringComparison.OrdinalIgnoreCase;
        private Dictionary<string /* prefix */, WoWindow> m_ActiveCameras = new Dictionary<string, WoWindow>();

        public WoService()
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
                Debug.Error("SLAVE: Unknown action received: " + action);
                break;
            }
        }

		private void OnConnectionLost()
        {
            // Exit application when connection is lost
            Debug.Error("Connection lost. OverlayManager shutting down.");
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Debug.Warning("Debugger is attached. Not exiting application.");
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

		private void RegisterCamera(JObject jObj)
        {
            if (!jObj.TryGetValue("cameraId", IGNORE,
                out var camIdToken))
            {
                Debug.Error("RegisterCamera missing cameraId");
                return;
            }

			var cameraId = camIdToken.Value<int>();

            var prefix = $"DwCamera_{cameraId}";
            // Check if camera is already registered
            if (m_ActiveCameras.ContainsKey(prefix))
            {
                Debug.Error($"Camera {cameraId} is already registered");
                return;
            }
            
            try
            {
                // Create DwForm for the camera
                var win = new WoWindow(cameraId);
                m_ActiveCameras.Add(prefix, win);
                
                // Show the form
                win.Show();
                win.Topmost = true;
                Debug.Warning($"Camera {cameraId} registered successfully");
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to register camera {cameraId}: {ex.Message}");
            }
        }

        private void UnregisterCamera(JObject jObj)
        {
            if (!jObj.TryGetValue("cameraId", IGNORE,
                out var camIdToken))
            {
                Debug.Error("UnregisterCamera missing cameraId");
                return;
            }
            var cameraId = camIdToken.Value<int>();
            
            UnregisterCamera(cameraId);
        }

        private void UnregisterCamera(int cameraId)
        {
            var prefix = $"DwCamera_{cameraId}";
            if (m_ActiveCameras.TryGetValue(prefix, out var WoWin))
            {
                try
                {
                    // Close and dispose the form
                    WoWin.Close();
                    WoWin.Dispose();
                    m_ActiveCameras.Remove(prefix);
                    Debug.Warning($"Camera {cameraId} unregistered successfully");
                }
                catch (Exception ex)
                {
                    Debug.Error($"Failed to unregister camera {cameraId}: {ex.Message}");
                }
            }
            else
            {
                Debug.Warning($"Camera {cameraId} was not registered");
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