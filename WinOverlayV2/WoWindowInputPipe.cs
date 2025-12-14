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
        private readonly string pipeName;
        private readonly WoWindow window;
        private NamedPipeServerStream pipeServer;
        private bool isRunning;
        private CancellationToken cancellationToken;

        public WoWindowInputPipe(string pipeName, WoWindow window)
        {
            this.pipeName = pipeName;
            this.window = window;
            
            // Subscribe to window events
            window.MouseDown += Window_MouseDown;
            window.MouseUp += Window_MouseUp;
            window.MouseMove += Window_MouseMove;
            window.MouseWheel += Window_MouseWheel;
            window.KeyDown += Window_KeyDown;
            window.KeyUp += Window_KeyUp;
        }

        public void Start(CancellationToken token)
        {
            this.cancellationToken = token;
            isRunning = true;
            Task.Run(() => ListenForMessages(), cancellationToken);
        }

        private async Task ListenForMessages()
        {
            while (isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    pipeServer = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await pipeServer.WaitForConnectionAsync(cancellationToken);

                    // Read messages from Unity
                    byte[] buffer = new byte[4096];
                    int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead > 0)
                    {
                        // Process message from Unity side
                        ProcessUnityMessage(buffer, bytesRead);
                    }

                    pipeServer.Disconnect();
                    pipeServer.Dispose();
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
            var pos = e.GetPosition(window);
            return pos.X >= 0 && pos.X <= window.ActualWidth &&
                   pos.Y >= 0 && pos.Y <= window.ActualHeight;
        }

        private bool TryCreateMouseEvent(int state, MouseEventArgs e, out MouseEventP3 mouseEvent)
        {
            mouseEvent = MouseEventP3.Invalid;

            var point = e.GetPosition(window);
            var screenPos = window.PointToScreen(point);
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
			if (!window.CameraShare.TryRead(out var camInfo))
				return false;

            var withinForm = window.IsContain(point);

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
                if (pipeServer != null && pipeServer.IsConnected)
                {
                    byte[] data = mouseEvent.ToByteArray();
                    pipeServer.Write(data, 0, data.Length);
                    pipeServer.Flush();
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
                if (pipeServer != null && pipeServer.IsConnected)
                {
                    byte[] data = keyEvent.ToByteArray();
                    pipeServer.Write(data, 0, data.Length);
                    pipeServer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending key event: {ex.Message}");
            }
        }

        public void Dispose()
        {
            isRunning = false;
            
            // Unsubscribe from window events
            if (window != null)
            {
                window.MouseDown -= Window_MouseDown;
                window.MouseUp -= Window_MouseUp;
                window.MouseMove -= Window_MouseMove;
                window.MouseWheel -= Window_MouseWheel;
                window.KeyDown -= Window_KeyDown;
                window.KeyUp -= Window_KeyUp;
            }

            pipeServer?.Dispose();
            pipeServer = null;
        }
    }
}
