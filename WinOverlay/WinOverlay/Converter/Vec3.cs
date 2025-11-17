using System;
using System.Collections;
using System.Collections.Generic;

namespace WinOverlay
{
	public partial class Vec3
	{
		public Vec3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
		public static readonly Vec3 One = new Vec3(1f, 1f, 1f);

		public static Vec3 operator +(Vec3 a, Vec3 b)
		{
			return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static Vec3 operator -(Vec3 a, Vec3 b)
		{
			return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}
		public static Vec3 operator *(Vec3 a, float b)
		{
			return new Vec3(a.X * b, a.Y * b, a.Z * b);
		}
		public static Vec3 operator /(Vec3 a, float b)
		{
			return new Vec3(a.X / b, a.Y / b, a.Z / b);
		}
		public static Vec3 operator *(float a, Vec3 b)
		{
			return new Vec3(a * b.X, a * b.Y, a * b.Z);
		}
		public static Vec3 operator /(float a, Vec3 b)
		{
			return new Vec3(a / b.X, a / b.Y, a / b.Z);
		}
		public static Vec3 operator -(Vec3 a)
		{
			return new Vec3(-a.X, -a.Y, -a.Z);
		}
		public static bool operator ==(Vec3 a, Vec3 b)
		{
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}
		public static bool operator !=(Vec3 a, Vec3 b)
		{
			return !(a == b);
		}
		
		public Vec3 Dot(Vec3 other)
		{
			return new Vec3(X * other.X, Y * other.Y, Z * other.Z);
		}

		public float Magnitude() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
		public float SqrMagnitude() => X * X + Y * Y + Z * Z;
		public Vec3 Normalize()
		{
			float mag = Magnitude();
			if (mag == 0f)
			{
				return new Vec3(0f, 0f, 0f);
			}
			return new Vec3(X / mag, Y / mag, Z / mag);
		}
		public Vec3 Cross(Vec3 other)
		{
			return new Vec3(
				Y * other.Z - Z * other.Y,
				Z * other.X - X * other.Z,
				X * other.Y - Y * other.X
			);
		}
		public float Distance(Vec3 other) => (this - other).Magnitude();

	}
}
