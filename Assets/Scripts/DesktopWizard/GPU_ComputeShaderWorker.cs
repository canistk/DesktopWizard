using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DesktopWizard
{
	public class GPU_ComputeShaderWorker : GPUWorker
	{
		public static readonly int inputTexId		= Shader.PropertyToID("_InputTex");
		public static readonly int resultTexId		= Shader.PropertyToID("_ResultTex");
		public static readonly int widthId			= Shader.PropertyToID("_width");
		public static readonly int heightId			= Shader.PropertyToID("_height");
		public static readonly int lodId			= Shader.PropertyToID("_lod");
		public static readonly int invertId			= Shader.PropertyToID("_invert");
		public static readonly int boomParams02Id	= Shader.PropertyToID("_boomParams02");
		public static readonly int boomParams01Id	= Shader.PropertyToID("_boomParams01");
		public static readonly int fxaaParams01Id	= Shader.PropertyToID("_fxaaParams01");
		private RenderTexture uploadTexture = null;
		public readonly ComputeShader shader;
		public readonly int kernelIdx;

		public GPU_ComputeShaderWorker(DwCamera dwc, int subId, ComputeShader s) : base(dwc, subId)
		{
			this.kernelIdx = s.FindKernel("DwCameraGPUKernel");
			this.shader = s;
		}

		private void HandleRenderTextureSize(int _w, int _h)
		{
			if (width == _w && height == _h)
				return; // nothing changed.
			this.width = _w;
			this.height = _h;
			shader.SetInt(widthId, width);
			shader.SetInt(heightId, height);

			uploadTexture?.Release();
			uploadTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
			{
				name = "dw_upload",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Point,
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
				useMipMap = true, // for down sampling
				autoGenerateMips = true, // for down sampling
				hideFlags = HideFlags.DontSave,
			};
			uploadTexture.Create();

			renderTexture?.Release();
			renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
			{
				name = "dw_download",
				enableRandomWrite = true, // Enable UAV
				antiAliasing = 1, // no anti-aliasing, while `enableRandomWrite` is true.
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Point,
				useMipMap = false,
				autoGenerateMips = false,
				hideFlags = HideFlags.DontSave,
			};
			renderTexture.Create();
		}

		private bool m_Inited;
		private float m_LastLod;
		private bool m_LastInvert, m_LastGamma, m_LastFXAAQty, m_LastFXAADebug, m_LastBoomDebug;
		private Vector4 m_LastBoom01, m_LastBoom02, m_LastFXAA01;

		private void HandleSettingSync()
		{
			if (m_LastLod != dwc.m_Lod || !m_Inited)
			{
				shader.SetFloat(lodId, dwc.m_Lod);
				m_LastLod = dwc.m_Lod;
			}
			if (m_LastInvert != dwc.m_Invert || !m_Inited)
			{
				shader.SetBool(invertId, dwc.m_Invert);
				m_LastInvert = dwc.m_Invert;
			}
			const string s_GAMMA_BLENDING = "GAMMA_BLENDING";
			if (m_LastGamma != dwc.m_GammaSpace || !m_Inited)
			{
				m_LastGamma = dwc.m_GammaSpace;
				if (m_LastGamma)
					shader.EnableKeyword(s_GAMMA_BLENDING);
				else
					shader.DisableKeyword(s_GAMMA_BLENDING);
			}

			// Booming
			{
				var tmp = new Vector4(
					dwc.m_BoomConfig.iteration,
					Mathf.GammaToLinearSpace(dwc.m_BoomConfig.intensity),
					dwc.m_BoomConfig.sampleOffset,
					dwc.m_BoomConfig.bias
					);
				if (m_LastBoom01 != tmp)
				{
					shader.SetVector(boomParams01Id, tmp);
					m_LastBoom01 = tmp;
				}

				const string s_BOOM_DEBUG = "BOOM_DEBUG";
				if (m_LastBoomDebug != dwc.m_BoomConfig.debug)
				{
					m_LastBoomDebug = dwc.m_BoomConfig.debug;
					if (m_LastBoomDebug)
						shader.EnableKeyword(s_BOOM_DEBUG);
					else
						shader.DisableKeyword(s_BOOM_DEBUG);
				}
			}
			{
				// https://catlikecoding.com/unity/tutorials/advanced-rendering/bloom/
				var threshold = dwc.m_BoomConfig.threshold;
				var knee = dwc.m_BoomConfig.threshold * dwc.m_BoomConfig.softThreshold;
				var tmp = new Vector4(
					threshold,
					threshold - knee,
					2f * knee,
					0.25f / (knee + 0.00001f)
					);
				if (m_LastBoom02 != tmp)
				{
					shader.SetVector(boomParams02Id, tmp);
					m_LastBoom02 = tmp;
				}
			}

			// FXAA
			{
				var tmp = new Vector4(
					dwc.m_FXAAConfig.contrastThreshold,
					dwc.m_FXAAConfig.relativeThreshold,
					dwc.m_FXAAConfig.subpixelBlending,
					dwc.m_FXAAConfig.weight
				);
				if (m_LastFXAA01 != tmp)
				{
					shader.SetVector(fxaaParams01Id, tmp);
					m_LastFXAA01 = tmp;
				}

				const string s_LOW_QUALITY = "LOW_QUALITY";
				if (m_LastFXAAQty != dwc.m_FXAAConfig.lowQuality)
				{
					m_LastFXAAQty = dwc.m_FXAAConfig.lowQuality;
					if (m_LastFXAAQty)
						shader.EnableKeyword(s_LOW_QUALITY);
					else
						shader.DisableKeyword(s_LOW_QUALITY);
				}

				const string s_FXAA_DEBUG = "FXAA_DEBUG";
				if (m_LastFXAADebug != dwc.m_FXAAConfig.debug)
				{
					m_LastFXAADebug = dwc.m_FXAAConfig.debug;
					if (m_LastFXAADebug)
						shader.EnableKeyword(s_FXAA_DEBUG);
					else
						shader.DisableKeyword(s_FXAA_DEBUG);
				}

			}
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
			_renderer.sharedMaterial.mainTexture = _camera.targetTexture = uploadTexture;
			_camera.RenderDontRestore();
			// _camera.Render();

			// pass setting.
			HandleSettingSync();

			// bind render textures Input/output
			shader.SetTexture(kernelIdx, inputTexId, uploadTexture);  // upload to GPU
			shader.SetTexture(kernelIdx, resultTexId, renderTexture); // download from GPU

			if (!m_Inited)
				m_Inited = true;

			// Push to GPU execute.
			var gw = Mathf.CeilToInt(width * 0.125f); // divide 8
			var gh = Mathf.CeilToInt(height * 0.125f); // divide 8
			shader.Dispatch(kernelIdx, gw, gh, 1);
			// Updated RenderTexture in GPU
		}
		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					uploadTexture?.Release();
				}
				uploadTexture = null;
			}
			base.Dispose(disposing);
		}
	}

}