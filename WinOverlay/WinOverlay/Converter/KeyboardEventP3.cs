using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinOverlay
{
	public partial class KeyboardEventP3
	{
		public KeyboardEventP3(KeyEventArgs e, bool isKeyUp)
		{
			KeyCode = (int)e.KeyCode;
			Alt = e.Alt;
			Control = e.Control;
			Shift = e.Shift;
			Handled = e.Handled;
			SuppressKeyPress = e.SuppressKeyPress;
			IsKeyUp = isKeyUp;
		}
	}
}