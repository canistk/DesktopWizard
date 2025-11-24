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
			if (p is null) return Point.Empty;
			return new Point((int)p.X, (int)p.Y);
		}

		public static implicit operator Vec2(Point p)
		{
			return new Vec2 { X = p.X, Y = p.Y };
		}

		public static readonly Vec2 Zero = new Vec2(0f, 0f);
		public static readonly Vec2 One = new Vec2(1f, 1f);

		public static Vec2 operator +(Vec2 a, Vec2 b)
		{
			if (a is null || b is null) return Vec2.Zero;
			return new Vec2(a.X + b.X, a.Y + b.Y);
		}
		public static Vec2 operator -(Vec2 a, Vec2 b)
		{
			if (a is null || b is null) return Vec2.Zero;
			return new Vec2(a.X - b.X, a.Y - b.Y);
		}
		public static Vec2 operator *(Vec2 a, float b)
		{
			if (a is null) return Vec2.Zero;
			return new Vec2(a.X * b, a.Y * b);
		}
		public static Vec2 operator /(Vec2 a, float b)
		{
			if (a is null) return Vec2.Zero;
			return new Vec2(a.X / b, a.Y / b);
		}
		public static Vec2 operator *(float a, Vec2 b)
		{
			if (b is null) return Vec2.Zero;
			return new Vec2(a * b.X, a * b.Y);
		}
		public static Vec2 operator /(float a, Vec2 b)
		{
			if (b is null) return Vec2.Zero;
			return new Vec2(a / b.X, a / b.Y);
		}
		public static Vec2 operator -(Vec2 a)
		{
			if (a is null) return Vec2.Zero;
			return new Vec2(-a.X, -a.Y);
		}
		public static bool operator ==(Vec2 a, Vec2 b)
		{
			if (a is null || b is null) return false;
			return a.X == b.X && a.Y == b.Y;
		}
		public static bool operator !=(Vec2 a, Vec2 b)
		{
			if (a is null || b is null) return true;
			return !(a == b);
		}

		public float Dot(Vec2 v)
		{
			if (v is null) return 0f;
			return X * v.X + Y * v.Y;
		}
		public float Length()
		{
			return (float)Math.Sqrt(X * X + Y * Y);
		}
		public Vec2 Normalize()
		{
			float len = Length();
			if (len > 1e-6)
			{
				return this / len;
			}
			return Vec2.Zero;
		}
		public Vec2 Lerp(Vec2 v, float t)
		{
			if (v is null) return Vec2.Zero;
			return this * (1f - t) + v * t;
		}
		public Vec2 Clamp(Vec2 min, Vec2 max)
		{
			if (min is null || max is null) return Vec2.Zero;
			return new Vec2(
				Math.Max(min.X, Math.Min(max.X, X)),
				Math.Max(min.Y, Math.Min(max.Y, Y))
			);
		}
		public Vec2 Clamp01()
		{
			return Clamp(Vec2.Zero, Vec2.One);
		}
		public Vec2 Abs()
		{
			return new Vec2(Math.Abs(X), Math.Abs(Y));
		}
		public Vec2 Reflect(Vec2 normal)
		{
			if (normal is null) return Vec2.Zero;
			return this - 2f * this.Dot(normal) * normal;
		}
	}
}
