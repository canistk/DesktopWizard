using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DesktopWizard
{
	public abstract class GPUWorker : IDisposable
	{
		public readonly DwCamera dwc;
		public RenderTexture renderTexture = null;
		public int width { get; protected set; } = -1;
		public int height { get; protected set; } = -1;
		public bool IsDisposed { get; protected set; } = false;
		public GPUWorker(DwCamera dwc)
		{
			this.dwc = dwc;
		}
		public abstract void Execute(Renderer _renderer, Camera _camera, int _width, int _height);

		/// <summary>
		/// For specific platforms, Unity has the following specifications:
		/// On Direct3D-like devices, Unity returns a pointer to the base Texture type(ID3D11Resource on D3D11, ID3D12Resource on D3D12).
		/// On OpenGL-like devices, the GL Texture "name" is returned; cast the pointer to an integer type to get it.
		/// On Metal, Unity returns an id<MTLTexture> pointer.
		/// On Vulkan, Unity returns an VkImage pointer.
		/// On platforms that do not support native code plug-ins, this function always returns NULL.
		/// </summary>
		/// <param name="rtHandle"></param>
		/// <returns></returns>
		public bool TryGetNativeTextureHandle(out IntPtr rtHandle)
		{
			rtHandle = IntPtr.Zero;
			if (renderTexture == null)
				return false;
			rtHandle = renderTexture.GetNativeTexturePtr();
			return true;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					renderTexture?.Release();
				}
				renderTexture = null;
			}
			IsDisposed = true;
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		~GPUWorker()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Workaround for Unity's RenderTexture active state.
			// ISSUE : Releasing render texture that is set to be RenderTexture.active!
			if (renderTexture == RenderTexture.active)
			{
				RenderTexture.active = null;
			}

			// Release render texture and GPU worker.
			renderTexture?.Release();
			renderTexture = null;
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}