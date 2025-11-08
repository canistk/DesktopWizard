using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private SynchronizationContext _syncContext;
        
        private static readonly List<string> s_MessageCache = new List<string>();
        private static readonly object s_CacheLock = new object();
        
        private Unity3DConnector() 
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private NamedPipeServerStream pipeClient;
        private NamedPipeClientStream pipeServer;
        public event Action<string> MessageReceived;
        public event Action ConnectionEstablished;
        public event Action ConnectionLosted;

        private bool HandShaked => pipeClient?.IsConnected == true && pipeServer?.IsConnected == true;

        private const StringComparison IGNORE = StringComparison.OrdinalIgnoreCase;
        private CancellationTokenSource m_CancelSrc = null;
		
        public async Task ConnectAsync()
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
                SendWarning("Are we done yet?.");

                _syncContext.Post(_ => ConnectionEstablished?.Invoke(), null);
            }
            catch
            {
                OnConnectionLosted();
            }
            Console.WriteLine("[Unity3DConnector] ConnectAsync finished.");
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

            OnConnectionLosted();
        }

        private void OnMessageReceived(string message)
        {
            if (message == null || message.Length == 0)
                return;

            _syncContext.Post(_ => MessageReceived?.Invoke(message), null);
        }

        private void OnConnectionLosted()
        {
            Debug.Error("[Unity3DConnector] Connection losted !!!!");
            m_CancelSrc?.Cancel();
            m_CancelSrc?.Dispose();
			_syncContext.Post(_ => ConnectionLosted?.Invoke(), null);
        }

        public void SendMessage(MyAction action)
            => SendMessage(action.ToJson());

        public void SendMessage(string message)
        {
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

        public void Dispose()
        {
            lock (_lock)
            {
                m_CancelSrc?.Cancel();
                m_CancelSrc?.Dispose();
                pipeServer?.Dispose();
				pipeClient?.Dispose();
                pipeServer = null;
                pipeClient = null;

				lock (s_CacheLock)
                {
                    if (s_MessageCache.Count > 0)
                    {
                        Debug.Log($"[Unity3DConnector] Disposing with {s_MessageCache.Count} unsent cached messages.");
                        s_MessageCache.Clear();
                    }
                }
                
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
