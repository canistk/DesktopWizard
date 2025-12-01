using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinOverlay
{
	public partial class CameraInfo
	{
		public Vec2Int formOSPos => new Vec2Int(FormOSPosX, FormOSPosY);
		public Vec2Int osPos => new Vec2Int(OsPosX, OsPosY);
		public Vec3 monPos => new Vec3(MonPosX, MonPosY, 0f);
		public Vec3 formPos => new Vec3(FormPosX, FormPosY, 0f);
	}
}
