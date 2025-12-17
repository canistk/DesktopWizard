using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Share;

namespace WinOverlay
{
	/// <summary>
    /// The unique message pipe between WinOverlay and Unity3D.
	/// Singleton pattern.
	/// </summary>
	public class WoMessagePipe : IDisposable
    {
        private static WoMessagePipe s_Instance;
        private static readonly object s_Lock = new object();

        public static WoMessagePipe Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Lock)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = new WoMessagePipe();
                        }
                    }
                }
                return s_Instance;
            }
        }

		private readonly SemaphoreSlim m_SendSemaphore = new SemaphoreSlim(1, 1);
        private readonly SynchronizationContext m_SyncContext;
		private CancellationTokenSource m_Cts;
        
        private static readonly List<string> s_MessageCache = new List<string>();
        private static readonly object s_CacheLock = new object();
        
        private WoMessagePipe() 
        {
			// For WPF, use default synchronization context
			m_SyncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private NamedPipeServerStream m_PipeClient;
        private NamedPipeClientStream m_PipeServer;
        public event Action<string> MessageReceived;
        public event Action ConnectionEstablished;
        public event Action ConnectionLosted;

        private bool HandShaked => m_PipeClient?.IsConnected == true && m_PipeServer?.IsConnected == true;

        public void Connect()
		{
            if (m_Cts != null)
                return; // already connecting or connected
			m_Cts = new CancellationTokenSource();
			Task.Run(ConnectAsync, m_Cts.Token);
		}
		private async Task ConnectAsync()
        {
            var token = m_Cts.Token;
			try
            {
				m_PipeClient?.Dispose();
                m_PipeClient = new NamedPipeServerStream(
                    "WinOverlay",
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                m_PipeServer?.Dispose();
                m_PipeServer = new NamedPipeClientStream(
                    ".",
					"Unity3DServer",
                    PipeDirection.In,
                    PipeOptions.Asynchronous);
				Debug.Log("[Unity3DConnector] Waiting for connection...");

                var job0 = m_PipeClient.WaitForConnectionAsync(token);
                await job0;
				var job1 = m_PipeServer.ConnectAsync(token);
                await job1;
				//await Task.WhenAll(job0, job1);

				Debug.Log("[Unity3DConnector] Connection detected, start listen pipe...");
				_ = Task.Run(ListenForMessages, token);

                if (HandShaked)
                {
                    Debug.Log("[Unity3DConnector] Handshake succeeded !!!");
                }
                else
                {
                    Debug.Error("[Unity3DConnector] Handshake failed ????");

                }
				await FlushCachedMessages();
                Debug.Warning("WinOverlay started.");

                m_SyncContext.Post(_ => ConnectionEstablished?.Invoke(), null);
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
            var token = m_Cts.Token;
			while (m_PipeServer?.IsConnected == true)
            {
				try
                {
					int bytesRead = await m_PipeServer.ReadAsync(buffer, 0, buffer.Length, token);
                    if (token.IsCancellationRequested)
                        break;
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

            m_SyncContext.Post(_ => MessageReceived?.Invoke(message), null);
        }

        private void OnConnectionLosted()
        {
            Debug.Error("[Unity3DConnector] Connection losted !!!!");
            if (m_Cts != null && !m_Cts.IsCancellationRequested)
                m_Cts.Cancel();
			m_Cts?.Dispose();
            m_Cts = null;
			m_SyncContext.Post(_ => ConnectionLosted?.Invoke(), null);
        }

		#region Send Message
		public void SendMessage(MyAction action)
            => SendMessage(action.ToJson());

        public void SendMessage(string message)
        {
            if (IsDisposed)
                return;
			if (!(m_PipeClient?.IsConnected == true))
            {
				// Not connected, cache the message
				lock (s_CacheLock)
                {
                    s_MessageCache.Add(message);
                    Debug.Log($"[Unity3DConnector] Message cached (total: {s_MessageCache.Count}): {message.Substring(0, Math.Min(50, message.Length))}...");
                }
                return;
            }
            
            _ = Task.Run(() => InternalSentAsync(message), m_Cts.Token);
        }

        [System.Obsolete("Use Debug.Error/Warning/Log instead.",true)]
        public void SendError(string message)
		{
			if (IsDisposed)
				return;
            using var err = new MyAction(CMD.SlaveError);
            err.Add("message", message);
            SendMessage(err);
        }
		[System.Obsolete("Use Debug.Error/Warning/Log instead.", true)]
		public void SendWarning(string message)
		{
			if (IsDisposed)
				return;
            using var warn = new MyAction(CMD.SlaveWarning);
            warn.Add("message", message);
            SendMessage(warn);
        }
		[System.Obsolete("Use Debug.Error/Warning/Log instead.", true)]
		public void SendInfo(string message)
		{
			if (IsDisposed)
				return;
			using var info = new MyAction(CMD.SlaveInfo);
            info.Add("message", message);
            SendMessage(info);
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
                    await Task.Delay(10, m_Cts.Token);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[Unity3DConnector] Failed to send cached message: {ex.Message}");
                }
            }
            
            Debug.Log($"[Unity3DConnector] All cached messages flushed.");
        }

        
        private async Task InternalSentAsync(string message)
		{
			if (IsDisposed)
				return;
			if (m_PipeClient == null || !m_PipeClient.IsConnected)
				return;
            var token = m_Cts.Token;
			await m_SendSemaphore.WaitAsync(token);
			try
            {
                if (token.IsCancellationRequested)
                    return;
			    if (m_PipeClient == null || !m_PipeClient.IsConnected)
                    throw new Exception("Pipe disconnected");
                var data = Encoding.UTF8.GetBytes(message);
                await m_PipeClient.WriteAsync(data, 0, data.Length, token);
                await m_PipeClient.FlushAsync(token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Unity3DConnector] Send failed: {ex.Message}");
            }
            finally
            {
                m_SendSemaphore.Release();
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
					lock (s_Lock)
					{
                        if (m_PipeServer != null && m_PipeServer.IsConnected)
                            m_PipeServer.Close();
                        if (m_PipeClient != null && m_PipeClient.IsConnected)
                            m_PipeClient.Close();
                        if (m_Cts != null && !m_Cts.IsCancellationRequested)
                            m_Cts.Cancel();
						m_Cts?.Dispose();
						m_PipeServer?.Dispose();
						m_PipeClient?.Dispose();
					}
                    s_Instance?.Dispose();
				}
			}
            m_Cts = null;
			m_PipeServer = null;
			m_PipeClient = null;
			s_Instance = null;
		}
        ~WoMessagePipe()
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
