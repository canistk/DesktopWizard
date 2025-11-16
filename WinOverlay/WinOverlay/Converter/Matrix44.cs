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
	public partial class Matrix44
	{
		public float m01 => m_[0];
		public float m02 => m_[1];
		public float m03 => m_[2];
		public float m04 => m_[3];
		public float m11 => m_[4];
		public float m12 => m_[5];
		public float m13 => m_[6];
		public float m14 => m_[7];
		public float m21 => m_[8];
		public float m22 => m_[9];
		public float m23 => m_[10];
		public float m24 => m_[11];
		public float m31 => m_[12];
		public float m32 => m_[13];
		public float m33 => m_[14];
		public float m34 => m_[15];

		public Vec3 MultiplePoint(Vec3 point)
		{
			float x = point.X;
			float y = point.Y;
			float z = point.Z;
			float rx = m11 * x + m12 * y + m13 * z + m14;
			float ry = m21 * x + m22 * y + m23 * z + m24;
			float rz = m31 * x + m32 * y + m33 * z + m34;
			return new Vec3(rx, ry, rz);
		}

		public Vec3 MultiplyPoint3x4(Vec3 point)
		{
  			float x = point.X;
			float y = point.Y;
			float z = point.Z;
			float rx = m11 * x + m12 * y + m13 * z;
			float ry = m21 * x + m22 * y + m23 * z;
			float rz = m31 * x + m32 * y + m33 * z;
			return new Vec3(rx, ry, rz);
		}
	}
}
