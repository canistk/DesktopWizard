using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DesktopWizard
{
	public class GPU_RawWorker : GPUWorker
	{
		public GPU_RawWorker(DwCamera dwc, int subId) : base(dwc, subId) { }
		private void HandleRenderTextureSize(int _w, int _h)
		{
			if (width == _w && height == _h)
				return; // nothing changed.
			this.width = _w;
			this.height = _h;
			if (renderTexture != null)
				renderTexture.Release();

			renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
			{
				name = "dw_rs",
				// enableRandomWrite = true, // enable UAV
				antiAliasing = 8, // no anti-aliasing, while `enableRandomWrite` is true.
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
				useMipMap = false,
				autoGenerateMips = false,
				hideFlags = HideFlags.DontSave,
			};
			renderTexture.Create();
		}

		public override void Execute(Renderer _renderer, Camera _camera, int _width, int _height)
		{
			if (_renderer == null)
				throw new System.NullReferenceException();
			if (_camera == null)
				throw new System.NullReferenceException();

			if (width != _width || height != _height)
				HandleRenderTextureSize(_width, _height);

			// Capture current render into texture
			_renderer.sharedMaterial.mainTexture = _camera.targetTexture = renderTexture;
			// _camera.Render();
			_camera.RenderDontRestore();
		}

		protected override void Dispose(bool disposing)
		{

			base.Dispose(disposing);
		}
	}
}