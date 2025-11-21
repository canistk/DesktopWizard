using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WinOverlay
{
	public partial class MouseEventP3
	{
		public MouseEventP3(MouseEventArgs e)
		{
			Button = (int)e.Button;
			Clicks = e.Clicks;
			X = e.X;
			Y = e.Y;
			Delta = e.Delta;
		}
	}
}
