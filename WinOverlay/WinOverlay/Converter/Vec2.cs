using System;
using System.Drawing;

namespace WinOverlay
{
	public partial class Vec2
	{
		public Vec2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public static explicit operator Point(Vec2 p)
		{
			return new Point((int)p.X, (int)p.Y);
		}

		public static implicit operator Vec2(Point p)
		{
			return new Vec2 { X = p.X, Y = p.Y };
		}
	}
}
