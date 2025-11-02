using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace WinOverlay
{
    public class Unity3DConnector : IDisposable
    {
        private NamedPipeClientStream pipeClient;
        public event Action<string> MessageReceived;
        public event Action ConnectionEstablished;

        public bool IsConnected => pipeClient?.IsConnected == true;

        private bool m_IsStartupCompleteAck = false;
		public void ReceivedStartupComplete()
        {
            m_IsStartupCompleteAck = true;
		}
		public async Task<bool> ConnectAsync()
        {
            try
            {
                pipeClient?.Dispose();
                pipeClient = new NamedPipeClientStream(".", "DwCamera_Control", PipeDirection.InOut);

				await pipeClient.ConnectAsync(1000);
                _ = Task.Run(ListenForMessages);

                int retryCount = 2;
				while (!m_IsStartupCompleteAck && retryCount-- > 0)
                {
                    // wait for Listener to be ready
                    await Task.Delay(100);
                    StartupComplete();
                }

                if (!m_IsStartupCompleteAck)
                {
                    Dispose();
                    return false;
                }

                ConnectionEstablished?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void StartupComplete()
        {
			// Placeholder for any startup complete logic if needed
			//var message = new MyAction(OverlayManager.CMD.StartupComplete).ToJson();
            if (pipeClient == null || !pipeClient.IsConnected)
                return;
			var message = $"{{\"action\":\"{OverlayManager.CMD.StartupComplete}\"}}";
			var data = Encoding.UTF8.GetBytes(message);
			pipeClient.Write(data, 0, data.Length);
			pipeClient.Flush();
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
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(message);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

		public void SendMessage(MyAction action)
			=> SendMessage(action.ToJson());

		public void SendMessage(string message)
        {
            if (!IsConnected)
                return;
            try
            {
                var data = Encoding.UTF8.GetBytes(message);
                pipeClient.Write(data, 0, data.Length);
                pipeClient.Flush();
            }
            catch
            {
            }
        }

		public void Dispose()
        {
            pipeClient?.Dispose();
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