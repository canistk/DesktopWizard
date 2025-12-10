using System;
using System.Windows;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.EventSystems;
#else
using System.Numerics;
#endif
namespace Share
{
	[StructLayout(LayoutKind.Sequential)]
		public struct Vec2Int
		{
		public int X, Y;
		public Vec2Int(int x, int y)
		{
			X = x;
			Y = y;
		}

#if UNITY_EDITOR || UNITY_STANDALONE
		public static implicit operator Vector2Int(Vec2Int vec) => new Vector2Int(vec.X, vec.Y);
		public static implicit operator Vec2Int(Vector2Int vec)
		{
			return new Vec2Int
			{
				X = vec.x,
				Y = vec.y
			};
		}
#else
#endif
		public static implicit operator Point(Vec2Int p)
		{
			return new Point(p.X, p.Y);
		}

		public static implicit operator Vec2Int(Point p)
		{
			return new Vec2Int { X = Convert.ToInt32(p.X), Y = Convert.ToInt32(p.Y) };
		}

		public static explicit operator Vec2(Vec2Int p)
		{
			return new Vec2((float)p.X, (float)p.Y);
		}
		
		public static readonly Vec2Int Zero = new Vec2Int(0, 0);
		public static readonly Vec2Int One = new Vec2Int(1, 1);
		public static Vec2Int operator +(Vec2Int a, Vec2Int b)
		{
			return new Vec2Int(a.X + b.X, a.Y + b.Y);
		}
		public static Vec2Int operator -(Vec2Int a, Vec2Int b)
		{
			return new Vec2Int(a.X - b.X, a.Y - b.Y);
		}
		public static Vec2Int operator *(Vec2Int a, int b)
		{
			return new Vec2Int(a.X * b, a.Y * b);
		}
		public static Vec2Int operator /(Vec2Int a, int b)
		{
			return new Vec2Int(a.X / b, a.Y / b);
		}
		public static Vec2Int operator *(Vec2Int a, float b)
		{
			return new Vec2Int((int)(a.X * b), (int)(a.Y * b));
		}
		public static Vec2Int operator /(Vec2Int a, float b)
		{
			return new Vec2Int((int)(a.X / b), (int)(a.Y / b));
		}
		public static Vec2Int operator *(int a, Vec2Int b)
		{
			return new Vec2Int(a * b.X, a * b.Y);
		}
		public static Vec2Int operator /(int a, Vec2Int b)
		{
			return new Vec2Int(a / b.X, a / b.Y);
		}
		public static Vec2Int operator *(float a, Vec2Int b)
		{
			return new Vec2Int((int)(a * b.X), (int)(a * b.Y));
		}
		public static Vec2Int operator /(float a, Vec2Int b)
		{
			return new Vec2Int((int)(a / b.X), (int)(a / b.Y));
		}
		public static Vec2Int operator -(Vec2Int a)
		{
			return new Vec2Int(-a.X, -a.Y);
		}
		public static bool operator ==(Vec2Int a, Vec2Int b)
		{
			return a.X == b.X && a.Y == b.Y;
		}
		public static bool operator !=(Vec2Int a, Vec2Int b)
		{
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
			return X * other.X + Y * other.Y;
		}
		public int Cross(Vec2Int other)
		{
			return X * other.Y - Y * other.X;
		}

		public override bool Equals(object obj)
		{
			return obj is Vec2Int @int &&
				   X == @int.X &&
				   Y == @int.Y;
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
