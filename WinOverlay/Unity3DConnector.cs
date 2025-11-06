using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinOverlay
{
    public class Unity3DConnector : IDisposable
    {
        private static Unity3DConnector _instance;
        private static readonly object _lock = new object();

        public static Unity3DConnector Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Unity3DConnector();
                        }
                    }
                }
                return _instance;
            }
        }

        private Unity3DConnector() { }

        private NamedPipeClientStream pipeClient;
        public event Action<string> MessageReceived;
        public event Action ConnectionEstablished;
        public event Action ConnectionLosted;

        public bool IsConnected => m_HandShaked && pipeClient?.IsConnected == true;
        private const StringComparison IGNORE = StringComparison.OrdinalIgnoreCase;
		
		public async Task ConnectAsync()
        {
            try
            {
                pipeClient?.Dispose();
                pipeClient = new NamedPipeClientStream(".", "DwCamera_Control", PipeDirection.InOut);

				await pipeClient.ConnectAsync(1000);
                _ = Task.Run(ListenForMessages);

                // First handshake
                int retryCount = 2;
				while (!m_HandShaked && retryCount-- > 0)
                {
                    // wait for Listener to be ready
                    await Task.Delay(500);
                    InternalSent("Ping");
                }

				if (!m_HandShaked)
				{
					throw new Exception("Handshake failed.");
				}

				ConnectionEstablished?.Invoke();
            }
            catch
            {
                OnConnectionLosted();
			}
        }

        bool m_HandShaked = false;
        byte[] m_Buffer = new byte[1024];
		private async void ListenForMessages()
        {
			while (pipeClient?.IsConnected == true)
            {
                try
                {
                    int bytesRead = await pipeClient.ReadAsync(m_Buffer, 0, m_Buffer.Length);

					if (bytesRead > 0)
                    {
                        var msg = Encoding.UTF8.GetString(m_Buffer, 0, bytesRead);
                        OnMessageReceived(msg);
					}
                    // TODO: support message over 1024.
				}
                catch
                {
                    break;
                }
            }

            OnConnectionLosted();
		}

        private void OnMessageReceived(string message)
        {
            if (message == null || message.Length == 0)
                return;

			if (message.Equals("Ping", IGNORE))
			{
				InternalSent("Pong");
                return;
			}
            if (message.Equals("Pong", IGNORE))
            {
                m_HandShaked = true;
                return;
			}

			MessageReceived?.Invoke(message);
		}

        private void OnConnectionLosted()
        {
            m_HandShaked = false;
			ConnectionLosted?.Invoke();
        }

		public void SendMessage(MyAction action)
			=> SendMessage(action.ToJson());

		public void SendMessage(string message)
        {
            if (!IsConnected)
                return;
            InternalSent(message);
        }

        public void SendError(string message)
        {
            using (var err = new MyAction(OverlayManager.CMD.SlaveError))
            {
                err.Add("message", message);
                SendMessage(err);
            }
		}
        public void SendWarning(string message)
        {
            using (var warn = new MyAction(OverlayManager.CMD.SlaveWarning))
            {
                warn.Add("message", message);
                SendMessage(warn);
            }
		}

        public void SendInfo(string message)
        {
            using (var info = new MyAction(OverlayManager.CMD.SlaveInfo))
            {
                info.Add("message", message);
                SendMessage(info);
            }
		}

		private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
		private void InternalSent(string message)
        {
            Task.Run(async () =>
            {
                if (pipeClient == null || !pipeClient.IsConnected)
                    return;
                await _sendSemaphore.WaitAsync();
                try
                {
                    var data = Encoding.UTF8.GetBytes(message);
                    await pipeClient.WriteAsync(data, 0, data.Length);
                    await pipeClient.FlushAsync();
                }
                catch (System.Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
                finally
                {
                    _sendSemaphore.Release();
				}
			});
        }

        public void Dispose()
        {
            lock (_lock)
            {
                pipeClient?.Dispose();
                _instance = null;
            }
        }

        public static void DisposeInstance()
        {
            lock (_lock)
            {
                _instance?.Dispose();
            }
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