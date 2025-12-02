using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinOverlay
{
	// partial class from Protobuf generated code
	// 16 float values for a 4x4 matrix
	// reference: message Matrix44 { repeated float m = 1; }
	public struct Mat4x4
	{
		public float[] M;
		// Matrix element accessors (column-major order to match Unity)
		// m_ array layout: [col0, col1, col2, col3]
		// Each column has 4 elements: [row0, row1, row2, row3]
		public float m00 => M.Length > 0 ? M[0] : 0f;   // column 0, row 0
		public float m10 => M.Length > 1 ? M[1] : 0f;   // column 0, row 1
		public float m20 => M.Length > 2 ? M[2] : 0f;   // column 0, row 2
		public float m30 => M.Length > 3 ? M[3] : 0f;   // column 0, row 3
		
		public float m01 => M.Length > 4 ? M[4] : 0f;   // column 1, row 0
		public float m11 => M.Length > 5 ? M[5] : 0f;   // column 1, row 1
		public float m21 => M.Length > 6 ? M[6] : 0f;   // column 1, row 2
		public float m31 => M.Length > 7 ? M[7] : 0f;   // column 1, row 3
		
		public float m02 => M.Length > 8 ? M[8] : 0f;   // column 2, row 0
		public float m12 => M.Length > 9 ? M[9] : 0f;   // column 2, row 1
		public float m22 => M.Length > 10 ? M[10] : 0f; // column 2, row 2
		public float m32 => M.Length > 11 ? M[11] : 0f; // column 2, row 3
		
		public float m03 => M.Length > 12 ? M[12] : 0f; // column 3, row 0 (translation X)
		public float m13 => M.Length > 13 ? M[13] : 0f; // column 3, row 1 (translation Y)
		public float m23 => M.Length > 14 ? M[14] : 0f; // column 3, row 2 (translation Z)
		public float m33 => M.Length > 15 ? M[15] : 0f; // column 3, row 3

		// Static factory methods
		public static readonly Mat4x4 Identity = new Mat4x4(System.Numerics.Matrix4x4.Identity);
		
		public static readonly Mat4x4 Zero = default;

		public static Mat4x4 CreateTranslation(Vec3 position)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateTranslation(position.X, position.Y, position.Z));
		}

		public static Mat4x4 CreateTranslation(float x, float y, float z)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateTranslation(x, y, z));
		}

		public static Mat4x4 CreateScale(Vec3 scale)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z));
		}

		public static Mat4x4 CreateScale(float x, float y, float z)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateScale(x, y, z));
		}

		public static Mat4x4 CreateScale(float scale)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateScale(scale));
		}

		public static Mat4x4 CreateRotationX(float radians)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateRotationX(radians));
		}

		public static Mat4x4 CreateRotationY(float radians)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateRotationY(radians));
		}

		public static Mat4x4 CreateRotationZ(float radians)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateRotationZ(radians));
		}

		public static Mat4x4 CreateFromYawPitchRoll(float yaw, float pitch, float roll)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, roll));
		}

		public static Mat4x4 CreateLookAt(Vec3 cameraPosition, Vec3 cameraTarget, Vec3 cameraUpVector)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateLookAt(
				new System.Numerics.Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z),
				new System.Numerics.Vector3(cameraTarget.X, cameraTarget.Y, cameraTarget.Z),
				new System.Numerics.Vector3(cameraUpVector.X, cameraUpVector.Y, cameraUpVector.Z)));
		}

		public static Mat4x4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance));
		}

		public static Mat4x4 CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
		{
			return new Mat4x4(System.Numerics.Matrix4x4.CreateOrthographic(width, height, zNearPlane, zFarPlane));
		}

		public static Mat4x4 CreateTRS(Vec3 position, Vec3 rotation, Vec3 scale)
		{
			var translation = CreateTranslation(position);
			var rotationMatrix = CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
			var scaleMatrix = CreateScale(scale);
			return translation.Multiply(rotationMatrix).Multiply(scaleMatrix);
		}

		// Matrix operations
		public Mat4x4 Multiply(Mat4x4 other)
		{
			var m1 = ToNumericsMatrix();
			var m2 = other.ToNumericsMatrix();
			return new Mat4x4(System.Numerics.Matrix4x4.Multiply(m1, m2));
		}

		public Mat4x4 Transpose()
		{
			return new Mat4x4(System.Numerics.Matrix4x4.Transpose(ToNumericsMatrix()));
		}

		public Mat4x4 Invert()
		{
			var numericsMatrix = ToNumericsMatrix();
			System.Numerics.Matrix4x4 inverted;
			if (System.Numerics.Matrix4x4.Invert(numericsMatrix, out inverted))
			{
				return new Mat4x4(inverted);
			}
			return Mat4x4.Zero; // Matrix is not invertible
		}

		public float GetDeterminant()
		{
			return ToNumericsMatrix().GetDeterminant();
		}

		// Transform point with full 4x4 transformation (including translation)
		public Vec3 MultiplyPoint(Vec3 point)
		{
			float x = point.X;
			float y = point.Y;
			float z = point.Z;
			float rx = m00 * x + m01 * y + m02 * z + m03;
			float ry = m10 * x + m11 * y + m12 * z + m13;
			float rz = m20 * x + m21 * y + m22 * z + m23;
			return new Vec3(rx, ry, rz);
		}

		// Transform point with 3x4 matrix (no perspective division)
		public Vec3 MultiplyPoint3x4(Vec3 point)
		{
  			float x = point.X;
			float y = point.Y;
			float z = point.Z;
			float rx = m00 * x + m01 * y + m02 * z + m03;
			float ry = m10 * x + m11 * y + m12 * z + m13;
			float rz = m20 * x + m21 * y + m22 * z + m23;
			return new Vec3(rx, ry, rz);
		}

		// Transform direction (ignore translation)
		public Vec3 MultiplyVector(Vec3 vector)
		{
			float x = vector.X;
			float y = vector.Y;
			float z = vector.Z;
			float rx = m00 * x + m01 * y + m02 * z;
			float ry = m10 * x + m11 * y + m12 * z;
			float rz = m20 * x + m21 * y + m22 * z;
			return new Vec3(rx, ry, rz);
		}

		// Transform extraction
		public Vec3 GetPosition()
		{
			return new Vec3(m03, m13, m23);
		}

		public Vec3 GetScale()
		{
			// In column-major: each column represents a basis vector
			float scaleX = (float)Math.Sqrt(m00 * m00 + m10 * m10 + m20 * m20);
			float scaleY = (float)Math.Sqrt(m01 * m01 + m11 * m11 + m21 * m21);
			float scaleZ = (float)Math.Sqrt(m02 * m02 + m12 * m12 + m22 * m22);
			return new Vec3(scaleX, scaleY, scaleZ);
		}

		public Vec3 GetRight()
		{
			// First column (X axis)
			return new Vec3(m00, m10, m20).Normalize();
		}

		public Vec3 GetUp()
		{
			// Second column (Y axis)
			return new Vec3(m01, m11, m21).Normalize();
		}

		public Vec3 GetForward()
		{
			// Third column (Z axis)
			return new Vec3(m02, m12, m22).Normalize();
		}

		// Utility methods
		public System.Numerics.Matrix4x4 ToNumericsMatrix()
		{
			// System.Numerics.Matrix4x4 is row-major
			// Convert from our column-major storage
			return new System.Numerics.Matrix4x4(
				m00, m01, m02, m03,  // row 0
				m10, m11, m12, m13,  // row 1
				m20, m21, m22, m23,  // row 2
				m30, m31, m32, m33   // row 3
			);
		}

		public bool IsIdentity()
		{
			return ToNumericsMatrix().IsIdentity;
		}

		public string ToMatrixString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"|{m00}, {m01}, {m02}, {m03}|");
			sb.AppendLine($"|{m10}, {m11}, {m12}, {m13}|");
			sb.AppendLine($"|{m20}, {m21}, {m22}, {m23}|");
			sb.AppendLine($"|{m30}, {m31}, {m32}, {m33}|");
			return sb.ToString();
		}

		// Constructor for System.Numerics.Matrix4x4 conversion
		public Mat4x4(System.Numerics.Matrix4x4 m)
		{
			// Convert from System.Numerics.Matrix4x4 (row-major) to column-major
			// System.Numerics: MRowColumn
			// Our storage: column-major [col0, col1, col2, col3]
			this.M = new float[16]
			{
				// Column 0 (X axis + first component of each row)
				m.M11, m.M21, m.M31, m.M41,
				// Column 1 (Y axis + second component of each row)
				m.M12, m.M22, m.M32, m.M42,
				// Column 2 (Z axis + third component of each row)
				m.M13, m.M23, m.M33, m.M43,
				// Column 3 (Translation + fourth component of each row)
				m.M14, m.M24, m.M34, m.M44
			};
		}
		public Mat4x4(float[] elements)
		{
			if (elements.Length != 16)
				throw new ArgumentException("Mat4x4 requires exactly 16 elements.");
			this.M = elements;
		}
	}
}
