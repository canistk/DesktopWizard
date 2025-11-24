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
			if (p is null) return Point.Empty;
			return new Point((int)p.X, (int)p.Y);
		}

		public static implicit operator Vec2Int(Point p)
		{
			return new Vec2Int { X = p.X, Y = p.Y };
		}

		public static explicit operator Vec2(Vec2Int p)
		{
			if (p is null) return new Vec2(0f, 0f);
			return new Vec2((float)p.X, (float)p.Y);
		}
		
		public static readonly Vec2Int Zero = new Vec2Int(0, 0);
		public static readonly Vec2Int One = new Vec2Int(1, 1);
		public static Vec2Int operator +(Vec2Int a, Vec2Int b)
		{
			if (a is null || b is null) return Vec2Int.Zero;
			return new Vec2Int(a.X + b.X, a.Y + b.Y);
		}
		public static Vec2Int operator -(Vec2Int a, Vec2Int b)
		{
			if (a is null || b is null) return Vec2Int.Zero;
			return new Vec2Int(a.X - b.X, a.Y - b.Y);
		}
		public static Vec2Int operator *(Vec2Int a, int b)
		{
			if (a is null) return Vec2Int.Zero;
			return new Vec2Int(a.X * b, a.Y * b);
		}
		public static Vec2Int operator /(Vec2Int a, int b)
		{
			if (a is null) return Vec2Int.Zero;
			return new Vec2Int(a.X / b, a.Y / b);
		}
		public static Vec2Int operator *(Vec2Int a, float b)
		{
			if (a is null) return Vec2Int.Zero;
			return new Vec2Int((int)(a.X * b), (int)(a.Y * b));
		}
		public static Vec2Int operator /(Vec2Int a, float b)
		{
			if (a is null) return Vec2Int.Zero;
			return new Vec2Int((int)(a.X / b), (int)(a.Y / b));
		}
		public static Vec2Int operator *(int a, Vec2Int b)
		{
			if (b is null) return Vec2Int.Zero;
			return new Vec2Int(a * b.X, a * b.Y);
		}
		public static Vec2Int operator /(int a, Vec2Int b)
		{
			if (b is null) return Vec2Int.Zero;
			return new Vec2Int(a / b.X, a / b.Y);
		}
		public static Vec2Int operator *(float a, Vec2Int b)
		{
			if (b is null) return Vec2Int.Zero;
			return new Vec2Int((int)(a * b.X), (int)(a * b.Y));
		}
		public static Vec2Int operator /(float a, Vec2Int b)
		{
			if (b is null) return Vec2Int.Zero;
			return new Vec2Int((int)(a / b.X), (int)(a / b.Y));
		}
		public static Vec2Int operator -(Vec2Int a)
		{
			if (a is null) return Vec2Int.Zero;
			return new Vec2Int(-a.X, -a.Y);
		}
		public static bool operator ==(Vec2Int a, Vec2Int b)
		{
			if (a is null || b is null) return false;
			return a.X == b.X && a.Y == b.Y;
		}
		public static bool operator !=(Vec2Int a, Vec2Int b)
		{
			if (a is null || b is null) return true;
			return a.X != b.X || a.Y != b.Y;
		}
		public Vec2Int Abs()
		{
			return new Vec2Int(Math.Abs(X), Math.Abs(Y));
		}
		public Vec2Int Sign()
		{
			return new Vec2Int(Math.Sign(X), Math.Sign(Y));
		}
		public int Dot(Vec2Int other)
		{
			if (other is null) return 0;
			return X * other.X + Y * other.Y;
		}
		public int Cross(Vec2Int other)
		{
			if (other is null) return 0;
			return X * other.Y - Y * other.X;
		}

	}
}
