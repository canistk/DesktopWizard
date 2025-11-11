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
			InitializeMemoryMappedFile();
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
		
		// ShareInfo MMF
		private MemoryMappedFile mmf = null;
		private MemoryMappedViewAccessor accessor = null;
		
		// Pixels MMF (new)
		private MemoryMappedFile mmfPixels = null;
		private MemoryMappedViewAccessor accessorPixels = null;
		
		// Temporary texture for ReadPixels
		private Texture2D m_TempTexture = null;
		
		private void InitializeMemoryMappedFile()
		{
			var shareName = $"DwCamera_{dwc.id}_{SubId}";
			var shareNamePixels = $"DwCamera_{dwc.id}_{SubId}_Pixels";
			
			// ShareInfo MMF
			var size = Marshal.SizeOf<ShareInfo>();
			mmf = MemoryMappedFile.CreateOrOpen(shareName, size);
			accessor = mmf.CreateViewAccessor(0, size, MemoryMappedFileAccess.Write);
			
			// Pixels MMF (initial size: 10MB)
			const long initialPixelBufferSize = 1024 * 1024 * 10; // 10MB
			mmfPixels = MemoryMappedFile.CreateOrOpen(shareNamePixels, initialPixelBufferSize);
			accessorPixels = mmfPixels.CreateViewAccessor(0, initialPixelBufferSize, MemoryMappedFileAccess.Write);
		}
		
		private void DisposeMemoryMappedFile()
		{
			// Dispose ShareInfo MMF
			accessor?.Dispose();
			mmf?.Dispose();
			accessor = null;
			mmf = null;
			
			// Dispose Pixels MMF
			accessorPixels?.Dispose();
			mmfPixels?.Dispose();
			accessorPixels = null;
			mmfPixels = null;
			
			// Dispose temp texture
			if (m_TempTexture != null)
			{
				UnityEngine.Object.Destroy(m_TempTexture);
				m_TempTexture = null;
			}
		}
		
		internal void UpdateMemory()
		{
			if (renderTexture == null)
				return;
			
			/*
			Note: renderTexture.GetNativeTexturePtr() documentation
			For specific platforms, Unity has the following specifications:
			On Direct3D-like devices, Unity returns a pointer to the base Texture type(ID3D11Resource on D3D11, ID3D12Resource on D3D12).
			On OpenGL-like devices, the GL Texture "name" is returned; cast the pointer to an integer type to get it.
			On Metal, Unity returns an id<MTLTexture> pointer.
			On Vulkan, Unity returns an VkImage pointer.
			On platforms that do not support native code plug-ins, this function always returns NULL.
			 */

			// 1. Read pixels using existing DumpTexture logic
			if (m_TempTexture == null || 
			    m_TempTexture.width != renderTexture.width || 
			    m_TempTexture.height != renderTexture.height)
			{
				if (m_TempTexture != null)
					UnityEngine.Object.Destroy(m_TempTexture);
				
				m_TempTexture = new Texture2D(
					renderTexture.width, 
					renderTexture.height, 
					TextureFormat.RGBA32, 
					mipChain: false, 
					linear: false)
				{
					wrapMode = TextureWrapMode.Clamp,
					filterMode = FilterMode.Point,
					hideFlags = HideFlags.DontSave,
				};
			}
			
			// 2. Read pixels from RenderTexture to Texture2D
			RenderTexture.active = renderTexture;
			m_TempTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
			m_TempTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			RenderTexture.active = null;
			
			// 3. Get raw pixel data
			byte[] pixels = m_TempTexture.GetRawTextureData();
			
			if (pixels != null && pixels.Length > 0)
			{
				// Resize MMF if needed
				if (accessorPixels.Capacity < pixels.Length)
				{
					accessorPixels?.Dispose();
					mmfPixels?.Dispose();
					
					var shareNamePixels = $"DwCamera_{dwc.id}_{SubId}_Pixels";
					long newSize = pixels.Length + (1024 * 1024); // Add 1MB buffer
					mmfPixels = MemoryMappedFile.CreateOrOpen(shareNamePixels, newSize);
					accessorPixels = mmfPixels.CreateViewAccessor(0, newSize, MemoryMappedFileAccess.Write);
				}
				
				// Write pixel data to MMF
				accessorPixels.WriteArray(0, pixels, 0, pixels.Length);
			}
			
			// 4. Update ShareInfo
			m_ShareInfo.rtHandler = renderTexture.GetNativeTexturePtr();
			m_ShareInfo.width = renderTexture.width;
			m_ShareInfo.height = renderTexture.height;
			m_ShareInfo.bytesPerPixel = 4; // RGBA32
			m_ShareInfo.rowPitch = renderTexture.width * 4;
			m_ShareInfo.totalSize = pixels?.Length ?? 0;
			m_ShareInfo.timestamp = DateTime.UtcNow;

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