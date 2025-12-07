using Share;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace DesktopWizard
{

	public class DwInputPipeReceiver : IDisposable
	{
		private NamedPipeClientStream m_Pipe;
		/// <summary>Mainthread dispatch <see cref="MainThread_Update"/></summary>
		public event Action<MouseEventP3> EVENT_Mouse;
		/// <summary>Mainthread dispatch <see cref="MainThread_Update"/></summary>
		public event Action<KeyboardEventP3> EVENT_Keyboard;
		public bool IsDisposed { get; private set; } = false;
		private byte[] m_Buffer = null;
		private Queue<IInputEvent> m_Events = new Queue<IInputEvent>(64);
		public DwInputPipeReceiver(string pipeName, int bufferSize = 1024 * 1024)
		{
			m_Buffer = new byte[bufferSize];
			m_Pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);
			Task.Run(ConnectToDwForm);
		}

		public void MainThread_Update()
		{
			while (m_Events.Count > 0)
			{
				var evt = m_Events.Dequeue();
				switch (evt)
				{
					case MouseEventP3 me: EVENT_Mouse?.Invoke(me); break;
					case KeyboardEventP3 ke: EVENT_Keyboard?.Invoke(ke); break;
				}
			}
		}

		private async Task ConnectToDwForm()
		{
			await m_Pipe.ConnectAsync();

			int bytesRead = -1;
			while (m_Pipe.IsConnected)
			{
				try
				{
					bytesRead = await m_Pipe.ReadAsync(m_Buffer, 0, m_Buffer.Length);
					if (bytesRead > 0)
					{
						// first 4 bytes into string UTF-8
						var tag = System.Text.Encoding.UTF8.GetString(m_Buffer, 0, 4);
						switch (tag)
						{
							case KeyboardEventP3.LABEL:
							var keyEvent = KeyboardEventP3.FromByteArray(ref m_Buffer);
							m_Events.Enqueue(keyEvent);
							break;

							case MouseEventP3.LABEL:
							var mouseEvent = MouseEventP3.FromByteArray(ref m_Buffer);
							m_Events.Enqueue(mouseEvent);
							break;

							default:
							Debug.LogError("Unknown input event tag: " + tag);
							break;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Error reading from input pipe: {ex.Message}");
				}
			}
			// End of connection.
			Dispose();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				IsDisposed = true;
				if (disposing)
				{
					m_Pipe?.Close();
					m_Pipe?.Dispose();
				}
				m_Buffer = null;
				m_Pipe = null;
			}
		}

		~DwInputPipeReceiver()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}