using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Share;

namespace WinOverlay
{

	public class WOInputPipe : System.IDisposable
	{
		public bool isDisposed { get; private set; }
		private NamedPipeServerStream m_Pipe = null;
		private CancellationToken m_CancelToken;
		public event Action OnClientConnected;
		public event Action OnClientDisconnected;

		public string name { get; private set; }
		public WOInputPipe(string pipName)
		{
			this.m_State = ePipeState.Disconnected;
			this.name = pipName;
			this.m_Pipe = new NamedPipeServerStream(name,
				PipeDirection.Out,
				1,
				PipeTransmissionMode.Byte,
				PipeOptions.Asynchronous);
			this.m_Semaphore = new SemaphoreSlim(1);
		}

		public void Start(CancellationToken token)
		{
			if (state != ePipeState.Disconnected)
				return;
			this.m_CancelToken = token;
			Task.Run(WaitForConnection, m_CancelToken);
		}

		#region Handling Connection State

		private ePipeState m_State = ePipeState.Disconnected;
		public ePipeState state
		{
			get => m_State;
			private set
			{
				if (m_State == value)
					return;

				if (m_Pipe == null)
				{
					Console.WriteLine($"[{name}] Pipe is null, when trying to set state = {m_State}->{value}.");
					return;
				}
				m_State = value;
				switch (value)
				{
					case ePipeState.Disconnected:
					{
						if (m_Pipe.IsConnected)
						{
							m_Pipe.Disconnect();
							Console.WriteLine($"[{name}] Self Disconnected.");
						}
						OnClientDisconnected?.Invoke();
					}
					break;
					case ePipeState.WaitingForConnection:
					{
						Console.WriteLine($"[{name}] Waiting for connection...");
					}
					break;
					case ePipeState.Connected:
					{
						if (!m_Pipe.IsConnected)
						{
							Console.WriteLine($"[{name}] Pipe is not connected after connection attempt.");
						}
						Console.WriteLine($"[{name}] Connected.");
						OnClientConnected?.Invoke();
					}
					break;
				}
			}
		}

		private async void WaitForConnection()
		{
			try
			{
				state = ePipeState.WaitingForConnection;
				int attempts = 0;
				while (m_Pipe == null && attempts++ < 5)
					await Task.Delay(100);
				if (m_Pipe == null)
					throw new Exception("Pipe is null before WaitForConnectionAsync.");
				await m_Pipe.WaitForConnectionAsync(m_CancelToken);
				state = ePipeState.Connected;
				while (m_Pipe != null &&
					m_Pipe.IsConnected &&
					!m_CancelToken.IsCancellationRequested)
				{
					await Task.Delay(100);
				}
				state = ePipeState.Disconnected;
			}
			catch (Exception ex)
			{
				if (ex is OperationCanceledException)
				{
					Console.WriteLine($"[{name}] Connection attempt cancelled.");
				}
				else
				{
					Console.WriteLine($"[{name}] Error in WaitForConnection: {ex.Message}");
				}
			}
			finally
			{
				state = ePipeState.Disconnected;
				Dispose();
			}
		}
		#endregion Handling Connection State

		#region Handling Keyboard/Mouse Events

		private SemaphoreSlim m_Semaphore = new SemaphoreSlim(1);
		public void Send(KeyEventArgs e, bool isKeyUp)
		{
			var data = new KeyboardEventP3(e, isKeyUp).ToByteArray();
			Task.Run(() => SendByte(data), m_CancelToken);
		}

		public void Send(int state, MouseEventArgs e, CameraInfo cam, bool withinForm)
		{
			var data = new MouseEventP3(state, e, cam, withinForm).ToByteArray();
			Task.Run(() => SendByte(data), m_CancelToken);
		}
		private async Task SendByte(byte[] data)
		{
			try
			{
				await m_Semaphore.WaitAsync();
				if (m_Pipe == null || !m_Pipe.IsConnected)
				{
					Console.WriteLine($"[{name}] Pipe is not connected when trying to send data.");
					return;
				}
				await m_Pipe.WriteAsync(data, 0, data.Length);
				m_Pipe.Flush();
			}
			finally
			{
				m_Semaphore?.Release();
			}
		}

		#endregion Handling Keyboard/Mouse Events

		#region Dispose Pattern
		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					m_Pipe?.Dispose();
					m_Semaphore?.Dispose();
				}
				m_Pipe = null;
				m_Semaphore = null;
				isDisposed = true;
			}
		}

		~WOInputPipe()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}
		#endregion Dispose Pattern
	}

	public enum ePipeState
	{
		Disconnected = 0,
		WaitingForConnection,
		Connected,
	}
}
