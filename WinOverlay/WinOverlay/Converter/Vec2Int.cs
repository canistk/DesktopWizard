using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinOverlay
{
	public partial class Vec2Int
	{
		public Vec2Int(int x, int y)
		{
			X = x;
			Y = y;
		}

		public static implicit operator Point(Vec2Int p)
		{
			return new Point((int)p.X, (int)p.Y);
		}

		public static implicit operator Vec2Int(Point p)
		{
			return new Vec2Int { X = p.X, Y = p.Y };
		}
	}
}
