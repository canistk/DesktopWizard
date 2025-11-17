using UnityEngine;
namespace Share
{
	// These are partial class definitions for protobuf-generated types.
	// The actual implementation is in Assets/Scripts/Generated/ShareClassUnity.cs
	// 
	// Unity type conversion extensions should be added in a separate file in the 
	// Assembly-CSharp assembly after the protobuf code generation is complete.
	
	public partial class Mat4x4
	{

		public Matrix4x4 ToMatrix4x4()
		{
			return new Matrix4x4(
				new Vector4(M[ 0], M[ 1], M[ 2], M[ 3]),
				new Vector4(M[ 4], M[ 5], M[ 6], M[ 7]),
				new Vector4(M[ 8], M[ 9], M[10], M[11]),
				new Vector4(M[12], M[13], M[14], M[15])
			);
		}

		public static implicit operator Matrix4x4(Mat4x4 mat) => mat.ToMatrix4x4();
		public static implicit operator Mat4x4(Matrix4x4 mat)
		{
			return new Mat4x4
			{
				M = {
					mat.m00, mat.m01, mat.m02, mat.m03,
					mat.m10, mat.m11, mat.m12, mat.m13,
					mat.m20, mat.m21, mat.m22, mat.m23,
					mat.m30, mat.m31, mat.m32, mat.m33
				}
			};
		}
	}

	public partial class Vec3
	{
		public Vector3 ToVector3() => new Vector3(X, Y, Z);
		public static implicit operator Vector3(Vec3 vec) => vec.ToVector3();
		public static implicit operator Vec3(Vector3 vec)
		{
			return new Vec3
			{
				X = vec.x,
				Y = vec.y,
				Z = vec.z
			};
		}
	}

	public partial class Vec2
	{
		public Vector2 ToVector2() => new Vector2(X, Y);
		public static implicit operator Vector2(Vec2 vec) => vec.ToVector2();
		public static implicit operator Vec2(Vector2 vec)
		{
			return new Vec2
			{
				X = vec.x,
				Y = vec.y
			};
		}
	}

	public partial class Vec2Int
	{
		public Vector2Int ToVector2Int() => new Vector2Int(X, Y);
		public static implicit operator Vector2Int(Vec2Int vec) => vec.ToVector2Int();
		public static implicit operator Vec2Int(Vector2Int vec)
		{
			return new Vec2Int
			{
				X = vec.x,
				Y = vec.y
			};
		}
	}
}