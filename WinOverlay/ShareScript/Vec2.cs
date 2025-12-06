using System;
using System.Drawing;
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.EventSystems;
#else
using System.Numerics;
#endif
namespace Share
{
	public struct Vec2
	{
		public float X, Y;
		public Vec2(float x, float y)
		{
			X = x;
			Y = y;
		}
		public static implicit operator Vector2(Vec2 vec) => new Vector2(vec.X, vec.Y);
		public static implicit operator Vec2(Vector2 vec)
		{
#if UNITY_EDITOR || UNITY_STANDALONE
			return new Vec2
			{
				X = vec.x,
				Y = vec.y
			};
#else
			return new Vec2
			{
				X = vec.X,
				Y = vec.Y
			};
#endif
		}

		private static void Fix(ref Vec2 v)
		{
			v.X = float.IsInfinity(v.X) || float.IsNaN(v.X) ? 0f : v.X;
			v.Y = float.IsInfinity(v.Y) || float.IsNaN(v.Y) ? 0f : v.Y;
		}

		public static explicit operator Point(Vec2 p)
		{
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
			Fix(ref a); Fix(ref b);
			return new Vec2(a.X + b.X, a.Y + b.Y);
		}
		public static Vec2 operator -(Vec2 a, Vec2 b)
		{
			Fix(ref a); Fix(ref b);
			return new Vec2(a.X - b.X, a.Y - b.Y);
		}
		public static Vec2 operator *(Vec2 a, float b)
		{
			Fix(ref a);
			return new Vec2(a.X * b, a.Y * b);
		}
		public static Vec2 operator /(Vec2 a, float b)
		{
			Fix(ref a);
			return new Vec2(a.X / b, a.Y / b);
		}
		public static Vec2 operator *(float a, Vec2 b)
		{
			Fix(ref b);
			return new Vec2(a * b.X, a * b.Y);
		}
		public static Vec2 operator /(float a, Vec2 b)
		{
			Fix(ref b);
			return new Vec2(a / b.X, a / b.Y);
		}
		public static Vec2 operator -(Vec2 a)
		{
			Fix(ref a);
			return new Vec2(-a.X, -a.Y);
		}
		public static bool operator ==(Vec2 a, Vec2 b)
		{
			Fix(ref a); Fix(ref b);
			return a.X == b.X && a.Y == b.Y;
		}
		public static bool operator !=(Vec2 a, Vec2 b)
		{
			Fix(ref a); Fix(ref b);
			return !(a == b);
		}

		public float Dot(Vec2 v)
		{
			Fix(ref v);
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
			Fix(ref v);
			return this * (1f - t) + v * t;
		}
		public Vec2 Clamp(Vec2 min, Vec2 max)
		{
			Fix(ref min); Fix(ref max);
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
			Fix(ref normal);
			return this - 2f * this.Dot(normal) * normal;
		}

		public override bool Equals(object obj)
		{
			return obj is Vec2 vec &&
				   X == vec.X &&
				   Y == vec.Y;
		}

		public override int GetHashCode()
		{
			int hashCode = 1861411795;
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			return hashCode;
		}
	}
}
