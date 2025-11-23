using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
    public class Unity3DConnector : IDisposable
    {
        private static Unity3DConnector s_Instance;
        private static readonly object _lock = new object();

        public static Unity3DConnector Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (_lock)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = new Unity3DConnector();
                        }
                    }
                }
                return s_Instance;
            }
        }

        private SynchronizationContext _syncContext;
        
        private static readonly List<string> s_MessageCache = new List<string>();
        private static readonly object s_CacheLock = new object();
        
        private Unity3DConnector() 
        {
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        }

        private NamedPipeServerStream pipeClient;
        private NamedPipeClientStream pipeServer;
        public event Action<string> MessageReceived;
        public event Action ConnectionEstablished;
        public event Action ConnectionLosted;

        private bool HandShaked => pipeClient?.IsConnected == true && pipeServer?.IsConnected == true;

        private CancellationTokenSource m_CancelSrc = null;

        public void Connect()
        {
            Task.Run(ConnectAsync);
		}
		private async Task ConnectAsync()
        {
            try
            {
                if (m_CancelSrc == null)
                {
                    m_CancelSrc = new CancellationTokenSource();
                }
                else
                {
                    m_CancelSrc.Cancel();
                    m_CancelSrc.Dispose();
                    m_CancelSrc = new CancellationTokenSource();
                }

				pipeClient?.Dispose();
                pipeClient = new NamedPipeServerStream(
                    "WinOverlay",
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                pipeServer?.Dispose();
                pipeServer = new NamedPipeClientStream(
                    ".",
					"Unity3DServer",
                    PipeDirection.In,
                    PipeOptions.Asynchronous);
				Debug.Log("[Unity3DConnector] Waiting for connection...");

                var job0 = pipeClient.WaitForConnectionAsync(m_CancelSrc.Token);
				var job1 = pipeServer.ConnectAsync(m_CancelSrc.Token);
                await Task.WhenAll(job0, job1);

				Debug.Log("[Unity3DConnector] Connection detected, start listen pipe...");
				_ = Task.Run(ListenForMessages);

                if (HandShaked)
                {
                    Debug.Log("[Unity3DConnector] Handshake succeeded !!!");
                }
                else
                {
                    Debug.Error("[Unity3DConnector] Handshake failed ????");

                }
				await FlushCachedMessages();
                SendWarning("WinOverlay started.");

                _syncContext.Post(_ => ConnectionEstablished?.Invoke(), null);
                Console.WriteLine("[Unity3DConnector] connection established.");
            }
            catch
            {
                OnConnectionLosted(); // fail before connection established
			}
		}

        private async void ListenForMessages()
        {
            var buffer = new byte[1024];
            
            while (pipeServer?.IsConnected == true)
            {
				try
                {
					int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        OnMessageReceived(msg);
                    }
                    // TODO: support message over 1024.
                }
                catch
                {
                    break;
                }
            }

            OnConnectionLosted(); // server disconnected
		}

        private void OnMessageReceived(string message)
        {
			if (IsDisposed ||
                message == null ||
                message.Length == 0)
                return;

            _syncContext.Post(_ => MessageReceived?.Invoke(message), null);
        }

        private void OnConnectionLosted()
        {
            Debug.Error("[Unity3DConnector] Connection losted !!!!, self disposing...");
            m_CancelSrc?.Cancel();
            m_CancelSrc?.Dispose();
			_syncContext.Post(_ => ConnectionLosted?.Invoke(), null);
        }

		#region Send Message
		public void SendMessage(MyAction action)
            => SendMessage(action.ToJson());

        public void SendMessage(string message)
        {
            if (IsDisposed)
                return;
            if (!(pipeClient?.IsConnected == true))
            {
                lock (s_CacheLock)
                {
                    s_MessageCache.Add(message);
                    Debug.Log($"[Unity3DConnector] Message cached (total: {s_MessageCache.Count}): {message.Substring(0, Math.Min(50, message.Length))}...");
                }
                return;
            }
            
            _ = Task.Run(() => InternalSentAsync(message));
        }

        public void SendError(string message)
		{
			if (IsDisposed)
				return;
			using (var err = new MyAction(OverlayManager.CMD.SlaveError))
            {
                err.Add("message", message);
                SendMessage(err);
            }
        }
        
        public void SendWarning(string message)
		{
			if (IsDisposed)
				return;
			using (var warn = new MyAction(OverlayManager.CMD.SlaveWarning))
            {
                warn.Add("message", message);
                SendMessage(warn);
            }
        }

        public void SendInfo(string message)
		{
			if (IsDisposed)
				return;
			using (var info = new MyAction(OverlayManager.CMD.SlaveInfo))
            {
                info.Add("message", message);
                SendMessage(info);
            }
        }

        private async Task FlushCachedMessages()
        {
            List<string> messagesToSend;
            
            lock (s_CacheLock)
            {
                if (s_MessageCache.Count == 0)
                    return;
                
                messagesToSend = new List<string>(s_MessageCache);
                s_MessageCache.Clear();
                
                Debug.Log($"[Unity3DConnector] Flushing {messagesToSend.Count} cached messages...");
            }
            
            foreach (var message in messagesToSend)
            {
                try
                {
                    await InternalSentAsync(message);
                    Debug.Log($"[Unity3DConnector] Cached message sent: {message.Substring(0, Math.Min(50, message.Length))}...");
                    
                    // 添加小延遲，避免消息發送過快
                    await Task.Delay(10);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[Unity3DConnector] Failed to send cached message: {ex.Message}");
                }
            }
            
            Debug.Log($"[Unity3DConnector] All cached messages flushed.");
        }

        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        
        private async Task InternalSentAsync(string message)
		{
			if (IsDisposed)
				return;
			if (pipeClient == null || !pipeClient.IsConnected)
                return;
			
			await _sendSemaphore.WaitAsync();
            try
            {
                var data = Encoding.UTF8.GetBytes(message);
                await pipeClient.WriteAsync(data, 0, data.Length);
                await pipeClient.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Unity3DConnector] Send failed: {ex.Message}");
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }
		#endregion Send Message

		#region Dispose Pattern
		public bool IsDisposed { get; private set; } = false;
		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				IsDisposed = true;
				if (disposing)
				{
					lock (s_CacheLock)
					{
						if (s_MessageCache.Count > 0)
						{
							Debug.Log($"[Unity3DConnector] Disposing with {s_MessageCache.Count} unsent cached messages.");
							s_MessageCache.Clear();
						}
					}
					lock (_lock)
					{
						m_CancelSrc?.Dispose();
						pipeServer?.Dispose();
						pipeClient?.Dispose();
					}
                    s_Instance?.Dispose();
				}
			}
            m_CancelSrc = null;
			pipeServer = null;
			pipeClient = null;
			s_Instance = null;
		}
        ~Unity3DConnector()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion Dispose Pattern
	}

	public class MyAction : Dictionary<string, object>, System.IDisposable
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
