using System;

#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.EventSystems;
#else
using System.Numerics;
#endif
namespace Share
{
	public struct Vec3
	{
		public float X, Y, Z;

		public Vec3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
		public static readonly Vec3 One = new Vec3(1f, 1f, 1f);

#if UNITY_EDITOR || UNITY_STANDALONE
		public static implicit operator Vector3(Vec3 vec) => new Vector3(vec.X, vec.Y, vec.Z);
		public static implicit operator Vec3(Vector3 vec)
		{
			return new Vec3
			{
				X = vec.x,
				Y = vec.y,
				Z = vec.z
			};
		}
#else
		public static implicit operator Vector3(Vec3 vec) => new Vector3(vec.X, vec.Y, vec.Z);
		public static implicit operator Vec3(Vector3 vec)
		{
			return new Vec3
			{
				X = Convert.ToSingle(vec.X),
				Y = Convert.ToSingle(vec.Y),
				Z = Convert.ToSingle(vec.Z)
			};
		}
#endif

		private static void Fix(ref Vec3 v)
		{
			v.X = float.IsInfinity(v.X) || float.IsNaN(v.X) ? 0f : v.X;
			v.Y = float.IsInfinity(v.Y) || float.IsNaN(v.Y) ? 0f : v.Y;
			v.Z = float.IsInfinity(v.Z) || float.IsNaN(v.Z) ? 0f : v.Z;
		}

		public static Vec3 operator +(Vec3 a, Vec3 b)
		{
			Fix(ref a); Fix(ref b);
			return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static Vec3 operator -(Vec3 a, Vec3 b)
		{
			Fix(ref a); Fix(ref b);
			return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}
		public static Vec3 operator *(Vec3 a, float b)
		{
			Fix(ref a);
			return new Vec3(a.X * b, a.Y * b, a.Z * b);
		}
		public static Vec3 operator /(Vec3 a, float b)
		{
			Fix(ref a);
			return new Vec3(a.X / b, a.Y / b, a.Z / b);
		}
		public static Vec3 operator *(float a, Vec3 b)
		{
			Fix(ref b);
			return new Vec3(a * b.X, a * b.Y, a * b.Z);
		}
		public static Vec3 operator /(float a, Vec3 b)
		{
			Fix(ref b);
			return new Vec3(a / b.X, a / b.Y, a / b.Z);
		}
		public static Vec3 operator -(Vec3 a)
		{
			Fix(ref a);
			return new Vec3(-a.X, -a.Y, -a.Z);
		}
		public static bool operator ==(Vec3 a, Vec3 b)
		{
			Fix(ref a); Fix(ref b);
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}
		public static bool operator !=(Vec3 a, Vec3 b)
		{
			Fix(ref a); Fix(ref b);
			return !(a == b);
		}
		
		public Vec3 Dot(Vec3 other)
		{
			Fix(ref other);
			return new Vec3(X * other.X, Y * other.Y, Z * other.Z);
		}

		public float Magnitude() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
		public float SqrMagnitude() => X * X + Y * Y + Z * Z;
		public Vec3 Normalize()
		{
			float mag = Magnitude();
			if (mag == 0f)
			{
				return Vec3.Zero;
			}
			return new Vec3(X / mag, Y / mag, Z / mag);
		}
		public Vec3 Cross(Vec3 other)
		{
			Fix(ref other);
			return new Vec3(
				Y * other.Z - Z * other.Y,
				Z * other.X - X * other.Z,
				X * other.Y - Y * other.X
			);
		}
		public float Distance(Vec3 other)
		{
			Fix(ref other);
			return (this - other).Magnitude();
		}

		public override bool Equals(object obj)
		{
			return obj is Vec3 vec &&
				   X == vec.X &&
				   Y == vec.Y &&
				   Z == vec.Z;
		}

		public override int GetHashCode()
		{
			int hashCode = -307843816;
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			hashCode = hashCode * -1521134295 + Z.GetHashCode();
			return hashCode;
		}
	}
}
