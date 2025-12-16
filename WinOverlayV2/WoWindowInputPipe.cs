using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Share;

namespace WinOverlay
{
    public class WoWindowInputPipe : IDisposable
    {
        private readonly string m_PipeName;
        private readonly WoWindow m_Window;
        private NamedPipeServerStream m_PipeServer;
        private CancellationTokenSource m_Cts;

		public WoWindowInputPipe(string pipeName, WoWindow window)
        {
            this.m_PipeName = pipeName;
            this.m_Window = window;
            
            // Subscribe to window events
            window.MouseDown += Window_MouseDown;
            window.MouseUp += Window_MouseUp;
            window.MouseMove += Window_MouseMove;
            window.MouseWheel += Window_MouseWheel;
            window.KeyDown += Window_KeyDown;
            window.KeyUp += Window_KeyUp;
			this.m_Cts = new CancellationTokenSource();
            Task.Run(() => ListenForMessages(), m_Cts.Token);
        }

        private async Task ListenForMessages()
        {
            while (!m_Cts.IsCancellationRequested)
            {
                try
                {
                    m_PipeServer = new NamedPipeServerStream(
                        m_PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await m_PipeServer.WaitForConnectionAsync(m_Cts.Token);
                    if (m_Cts.IsCancellationRequested)
                        break;

					// Read messages from Unity
					byte[] buffer = new byte[4096];
                    int bytesRead = await m_PipeServer.ReadAsync(buffer, 0, buffer.Length, m_Cts.Token);
					if (m_Cts.IsCancellationRequested)
						break;
					if (bytesRead > 0)
                    {
                        // Process message from Unity side
                        ProcessUnityMessage(buffer, bytesRead);
                    }

                    m_PipeServer.Disconnect();
                    m_PipeServer.Dispose();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"InputPipe error: {ex.Message}");
                    await Task.Delay(100);
                }
            }
        }

        private void ProcessUnityMessage(byte[] buffer, int length)
        {
            // This would handle messages from Unity if needed
            // For now, we're mainly sending events TO Unity
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsMouseWithinWindow(e))
                return;

            if (!TryCreateMouseEvent(0, e, out var mouseEvent))
                return;
            SendMouseEvent(mouseEvent);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsMouseWithinWindow(e))
                return;

			if (!TryCreateMouseEvent(1, e, out var mouseEvent))
				return;
			SendMouseEvent(mouseEvent);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsMouseWithinWindow(e))
                return;

			if (!TryCreateMouseEvent(2, e, out var mouseEvent))
				return;
			SendMouseEvent(mouseEvent);
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsMouseWithinWindow(e))
                return;

			if (!TryCreateMouseEvent(3, e, out var mouseEvent))
				return;
			mouseEvent.WheelDelta = e.Delta;
            SendMouseEvent(mouseEvent);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var keyEvent = CreateKeyEvent(false, e);
            SendKeyEvent(keyEvent);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            var keyEvent = CreateKeyEvent(true, e);
            SendKeyEvent(keyEvent);
        }

        private bool IsMouseWithinWindow(MouseEventArgs e)
        {
            var pos = e.GetPosition(m_Window);
            return pos.X >= 0 && pos.X <= m_Window.ActualWidth &&
                   pos.Y >= 0 && pos.Y <= m_Window.ActualHeight;
        }

        private bool TryCreateMouseEvent(int state, MouseEventArgs e, out MouseEventP3 mouseEvent)
        {
            mouseEvent = MouseEventP3.Invalid;

            var point = e.GetPosition(m_Window);
            var screenPos = m_Window.PointToScreen(point);
#if false
            var WinPos = new Vec2(Convert.ToSingle(window.Top), Convert.ToSingle(window.Left));
            var WinSize = new Vec2(Convert.ToSingle(window.Width), Convert.ToSingle(window.Height));
            var WinRect = new Rect(
                WinPos.X,
                WinPos.Y,
                WinSize.X,
                WinSize.Y);
            WinRect.Contains(point);
#else
#endif
			if (!m_Window.CameraShare.TryRead(out var camInfo))
				return false;

            var withinForm = m_Window.IsContain(point);

			var OsV3 = new Vec3(Convert.ToSingle(point.X), Convert.ToSingle(point.Y), 0);
            var monPos = camInfo.O2M.MultiplyPoint3x4(OsV3);
            var formPos = camInfo.M2F.MultiplyPoint3x4(monPos);


			mouseEvent = new MouseEventP3
            {
                State = state,
                Button = GetMouseButton(e),
                X = (int)point.X,
                Y = (int)point.Y,
                WheelDelta = 0,
                monX = monPos.X,
				monY = monPos.Y,
                formX = formPos.X,
                formY = formPos.Y,
				Clicks = (e is MouseButtonEventArgs mbe) ? mbe.ClickCount : 0,
                withinForm = withinForm,
                //Shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift),
                //Control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl),
                //Alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)
            };

            if (e is MouseWheelEventArgs wheelArgs)
            {
                mouseEvent.WheelDelta = wheelArgs.Delta;
            }
            return true;
		}

        private int GetMouseButton(MouseEventArgs e)
        {
            if (e is MouseButtonEventArgs buttonArgs)
            {
                switch (buttonArgs.ChangedButton)
                {
                    case MouseButton.Left:
                        return 0;
                    case MouseButton.Right:
                        return 1;
                    case MouseButton.Middle:
                        return 2;
                    case MouseButton.XButton1:
                        return 3;
                    case MouseButton.XButton2:
                        return 4;
                    default:
                        return 0;
				}
            }
            return 0;
        }

        private KeyboardEventP3 CreateKeyEvent(bool isKeyUp, KeyEventArgs e)
        {
            var keyEvent = new KeyboardEventP3
            {
                IsKeyUp = isKeyUp,
                KeyCode = (int)KeyInterop.VirtualKeyFromKey(e.Key),
                Shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift),
                Control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl),
                Alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)
            };

            return keyEvent;
        }

        private void SendMouseEvent(MouseEventP3 mouseEvent)
        {
            try
            {
                if (m_PipeServer != null && m_PipeServer.IsConnected)
                {
                    byte[] data = mouseEvent.ToByteArray();
                    m_PipeServer.Write(data, 0, data.Length);
                    m_PipeServer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending mouse event: {ex.Message}");
            }
        }

        private void SendKeyEvent(KeyboardEventP3 keyEvent)
        {
            try
            {
                if (m_PipeServer != null && m_PipeServer.IsConnected)
                {
                    byte[] data = keyEvent.ToByteArray();
                    m_PipeServer.Write(data, 0, data.Length);
                    m_PipeServer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending key event: {ex.Message}");
            }
        }

		#region Dispose Pattern
		public bool isDisposed { get; private set; } = false;
		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				isDisposed = true;
				if (disposing)
				{
					// Unsubscribe from window events
					if (m_Window != null)
					{
						m_Window.MouseDown  -= Window_MouseDown;
						m_Window.MouseUp    -= Window_MouseUp;
						m_Window.MouseMove  -= Window_MouseMove;
						m_Window.MouseWheel -= Window_MouseWheel;
						m_Window.KeyDown    -= Window_KeyDown;
						m_Window.KeyUp      -= Window_KeyUp;
					}

					m_PipeServer?.Dispose();
				}
                m_PipeServer = null;
			}
		}

        ~WoWindowInputPipe()
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
		#endregion Dispose Pattern
	}
}
