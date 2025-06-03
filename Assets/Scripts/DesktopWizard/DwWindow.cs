using Kit2;
using Kit2.Task;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DesktopWizard
{
    public class DwWindow : DwArea
    {
		public uint id => m_WindowInfo.id;
		public string title => m_WindowInfo.title;
		public WindowInfo m_WindowInfo;
		public void Init(WindowInfo windowInfo)
		{
			this.m_WindowInfo = windowInfo;
			this.gameObject.name = $"[Window] {windowInfo.title}";
			if (DwCore.layer.WindowLayer >= 0)
				this.gameObject.layer = DwCore.layer.WindowLayer;
			base.Init(this.m_WindowInfo.rect);
		}

		public DwCamera dwCamera;
		public void LinkCamera(DwCamera camera)
		{
			this.dwCamera = camera;
		}

		/// <summary>
		/// Move toward in Monitor space position
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void MoveTo_Monitor(float x, float y)
		{
			var p = new Vector3(x, y, 0f);
			MoveTo_Monitor(p);
		}

		/// <summary>
		/// Move toward in Monitor space position
		/// </summary>
		/// <param name="point">ignore Z axis.</param>
		/// <exception cref="System.ArgumentNullException"></exception>
		public void MoveTo_Monitor(Vector3 point)
		{
			if (dwCamera == null)
				throw new System.ArgumentNullException();
			var o = dwCamera.MatrixMonitorToOS().MultiplyPoint3x4(point);
			var x = (int)o.x;
			var y = (int)o.y;
			//var z = (int)o.z;
			dwCamera.dwForm.MoveTo_OS(x, y);
		}

	}
}