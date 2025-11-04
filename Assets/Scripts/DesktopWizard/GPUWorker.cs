using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using UnityEngine;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
namespace DesktopWizard
{
	public abstract class GPUWorker : IDisposable
	{
		public readonly DwCamera dwc;
		public RenderTexture renderTexture = null;
		public int width { get; protected set; } = -1;
		public int height { get; protected set; } = -1;
		public bool IsDisposed { get; protected set; } = false;
		public int SubId { get; protected set; } = -1;
		public GPUWorker(DwCamera dwc, int subId)
		{
			this.dwc = dwc;
			this.SubId = subId;
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

		#region Share Memory

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ShareInfo
		{
			public IntPtr rtHandler;
			public DateTime timestamp;
			public int width;
			public int height;
			public int rowPitch;
			public int bytesPerPixel;
			public int totalSize;
		}
		private ShareInfo m_ShareInfo;
		private MemoryMappedFile mmf = null;
		private MemoryMappedViewAccessor accessor = null;
		
		private void InitializeMemoryMappedFile()
		{
			accessor?.Dispose();
			mmf?.Dispose();

			var shareName = $"DwCamera_{dwc.id}_{SubId}";
			mmf = MemoryMappedFile.CreateOrOpen(shareName, m_ShareInfo.totalSize);
			var size = Marshal.SizeOf<ShareInfo>();
			accessor = mmf.CreateViewAccessor(0, size, MemoryMappedFileAccess.Write);
		}
		private void DisposeMemoryMappedFile()
		{
			accessor?.Dispose();
			mmf?.Dispose();
			accessor = null;
			mmf = null;
		}
		internal void UpdateMemory()
		{
			if (renderTexture == null)
				return;
			/*
			Note: renderTexture.GetNativeDepthBufferPtr() documentation
			For specific platforms, Unity has the following specifications:
			On Direct3D-like devices, Unity returns a pointer to the base Texture type(ID3D11Resource on D3D11, ID3D12Resource on D3D12).
			On OpenGL-like devices, the GL Texture "name" is returned; cast the pointer to an integer type to get it.
			On Metal, Unity returns an id<MTLTexture> pointer.
			On Vulkan, Unity returns an VkImage pointer.
			On platforms that do not support native code plug-ins, this function always returns NULL.
			 */
			
			m_ShareInfo.rtHandler		= renderTexture.GetNativeDepthBufferPtr();
			m_ShareInfo.width			= renderTexture.width;
			m_ShareInfo.height			= renderTexture.height;
			var bytesPerPixel			=
			m_ShareInfo.bytesPerPixel	= GetBytesPerPixel(renderTexture.format);

			var rowPitch				= 0;
			var newTotalSize			= 0;

			switch (SystemInfo.graphicsDeviceType)
			{
				case GraphicsDeviceType.Direct3D11:
				case GraphicsDeviceType.Direct3D12:
				// DirectX may require row pitch alignment (typically 256 bytes)
				rowPitch = AlignToPowerOfTwo(width * bytesPerPixel, 256);
				newTotalSize = height * rowPitch;
				break;

				case GraphicsDeviceType.OpenGLES2:
				case GraphicsDeviceType.OpenGLES3:
				case GraphicsDeviceType.OpenGLCore:
				// OpenGL typically doesn't require special alignment for texture data
				rowPitch = width * bytesPerPixel;
				newTotalSize = height * rowPitch;
				break;

				case GraphicsDeviceType.Metal:// Metal may require specific alignment
				rowPitch = AlignToPowerOfTwo(rowPitch, 64);
				newTotalSize = height * rowPitch;
				break;
				default:
				throw new NotSupportedException($"Graphics API {SystemInfo.graphicsDeviceType} not supported yet.");
			}

			m_ShareInfo.rowPitch		= m_ShareInfo.width * m_ShareInfo.bytesPerPixel;
			m_ShareInfo.timestamp		= DateTime.UtcNow;
			var isTotalSizeChanged		= m_ShareInfo.totalSize != newTotalSize;
			m_ShareInfo.totalSize		= newTotalSize;

			if (isTotalSizeChanged || // Re-initialize memory-mapped file if size changed
				accessor == null)
			{
				InitializeMemoryMappedFile();
			}
			// Write ShareInfo to memory-mapped file
			accessor.Write(0, ref m_ShareInfo);
		}

		private int AlignToPowerOfTwo(int value, int alignment)
		{
			return ((value + alignment - 1) / alignment) * alignment;
		}
		private int GetBytesPerPixel(RenderTextureFormat format)
		{
			return format switch
			{
				// 8-bit formats
				RenderTextureFormat.R8 => 1,
				RenderTextureFormat.RG16 => 2,
				RenderTextureFormat.RGB565 => 2,

				// 16-bit formats  
				RenderTextureFormat.ARGB4444 => 2,
				RenderTextureFormat.ARGB1555 => 2,
				RenderTextureFormat.RHalf => 2,
				RenderTextureFormat.RGHalf => 4,

				// 32-bit formats
				RenderTextureFormat.ARGB32 => 4,
				RenderTextureFormat.BGRA32 => 4,
				RenderTextureFormat.RFloat => 4,
				RenderTextureFormat.RGFloat => 8,
				RenderTextureFormat.RInt => 4,
				RenderTextureFormat.RGInt => 8,

				// 64-bit formats
				RenderTextureFormat.ARGBHalf => 8,
				RenderTextureFormat.RGBAUShort => 8,

				// 128-bit formats
				RenderTextureFormat.ARGBFloat => 16,
				RenderTextureFormat.ARGBInt => 16,

				// Depth formats
				RenderTextureFormat.Depth => 4,
				RenderTextureFormat.Shadowmap => 4,

				_ => throw new NotSupportedException($"RenderTexture format {format} not supported yet."),
			};
		}
		#endregion Share Memory

		#region Dispose
		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					DisposeMemoryMappedFile();
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
		#endregion Dispose
	}
}