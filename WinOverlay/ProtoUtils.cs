using System.Windows.Forms;

namespace WinOverlay
{
    public static class Utils
    {

        public static MouseEventP3 ToProtoBuf(this MouseEventArgs e)
        {
            return new MouseEventP3
            {
                Button = (int)e.Button,
                Clicks = e.Clicks,
                X = e.X,
                Y = e.Y,
                Delta = e.Delta,
                Location = new PointP3 { X = e.Location.X, Y = e.Location.Y }
            };
        }

        public static KeyboardEventP3 ToProtoBuf(this KeyEventArgs e, bool isKeyUp)
        {
            return new KeyboardEventP3
            {
                KeyData = (int)e.KeyData,
                KeyCode = (int)e.KeyCode,
                Alt = e.Alt,
                Control = e.Control,
                Shift = e.Shift,
                Handled = e.Handled,
                SuppressKeyPress = e.SuppressKeyPress,
                IsKeyUp = isKeyUp
            };
        }
    }
}
