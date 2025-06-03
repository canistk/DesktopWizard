#define TRY_CATCH
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Kit2;

namespace DesktopWizard
{
    using Size = System.Drawing.Size;
    using Bitmap = System.Drawing.Bitmap;
    using InputButton = PointerEventData.InputButton;
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(Camera))]
    public class DwCamera : MonoBehaviour
	{
        [System.Flags]
        public enum eDebug
        {
            LogMouseEvent   = 1 << 0,
            DrawMouseRayFormSpace = 1 << 1,
			LogKeyEvent     = 1 << 2,
			DrawClickPosMonitorSpace = 1 << 3,
			DrawClickPosFormSpace = 1 << 4,
			LogUIEvents = 1 << 5,
		}
        [System.Serializable]
        public struct Config
        {
            public eDebug debug;
        }

        [SerializeField]
        public Config m_Config = default;

        public enum AntiAliasingType
        {
            None = 1,
            TwoSamples = 2,
            FourSamples = 4,
            EightSamples = 8,
            S16Samples = 16,
			S32Samples = 32,
		}

        public enum UpdateFuncType
        {
            Update = 1,
            LateUpdate = 2,
        }

		#region Opacity
		private class DefaultOpacityModifier : IPriorityObj
		{
            private DwCamera m_Src;
            public DefaultOpacityModifier(DwCamera owner) => m_Src = owner;
            public float Priority => float.MinValue;

			public object Value => Mathf.Clamp01(m_Src.m_OpacityVal);

			public int CompareTo(IPriorityObj other) => Priority.CompareTo(other.Priority);

			public bool Equals(IPriorityObj other) => this == other;
		}
		private DefaultOpacityModifier m_DefaultOpacityModifier;
        private DefaultOpacityModifier defaultOpacityModifier
        {
            get
            {
                if (m_DefaultOpacityModifier == null)
                    m_DefaultOpacityModifier = new DefaultOpacityModifier(this);
                return m_DefaultOpacityModifier;
            }
        }
        private PriorityList<IPriorityObj> m_OpacityPriorityList;
        private PriorityList<IPriorityObj> opacityPriority
		{
            get
            {
				if (m_OpacityPriorityList == null)
                    m_OpacityPriorityList = new PriorityList<IPriorityObj>(descending:true);
                if (m_OpacityPriorityList.Count == 0)
                    m_OpacityPriorityList.Add(defaultOpacityModifier);
                return m_OpacityPriorityList;
            }
        }
        public void AddOpacityModifier(IPriorityObj obj) => opacityPriority.Add(obj);
        public int RemoveOpacityModifier(IPriorityObj obj) => opacityPriority.Remove(obj);

		public float Opacity
		{
			get
            {
				if (!opacityPriority.TryPeek(out var ctrl))
                    return 0;

                var value = (float)ctrl.Value;
				return value;
            }
            set { m_OpacityVal = Mathf.Clamp01(value); }
        }
		#endregion Opacity

		public string Title
        {
            get => dwForm == null ? string.Empty : dwForm.Text;
            set
            {
                if (dwForm != null)
                    dwForm.Text = value;
            }
        }

        public int Left
        {
            get
            {
                if (dwForm == null)
                    return setting.StartPos.x;

                return dwForm.Left;
            }
            set
            {
				if (dwForm == null)
				{
					setting.StartPos = new Vector2Int(value, Top);
					return;
				}

				dwForm.Left = value;
            }
        }

        /// <summary>
        /// Note : 
        /// U3D's y-axis is flipped, upward = y++, downward = y--, left-bottom = 0,0
        /// Window's y-axis is, upward = y--, downward = y++, left-top = 0,0
        /// </summary>
        public int Top
        {
            get
            {
                if (dwForm == null)
                    return setting.StartPos.y;

                return dwForm.Top;
            }
            set
            {
                if (dwForm == null)
                {
                    setting.StartPos = new Vector2Int(Left, value);
					return;
                }
                
                dwForm.Top = value;
            }
        }

        public int Width
        {
            get
            {
                if (dwForm == null)
                    return setting.Size.x;
                return dwForm.Width; // sync after update
            } 
            set { if (dwForm != null) setting.Size.x = value; }
        }

        public int Height
        {
            get
            {
                if (dwForm == null)
                    return setting.Size.y;
                return dwForm.Height; // sync after update
            }
            set { if (dwForm != null) setting.Size.y = value; }
        }

		/// <summary>
		/// A window info is a value that uniquely identifies a window within a desktop.
		/// it depended on Window-Form handle (HWND) on created.
		/// Note: it may changed after window is closed and re-created.
		/// </summary>
		/// <param name="windowInfo"></param>
		/// <returns></returns>
		public bool TryGetWindowInfo(out WindowInfo windowInfo)
		{
			if (dwForm == null)
			{
				windowInfo = default;
				return false;
			}

            // An System.IntPtr that contains the window handle (HWND) of the control.
            var id = (uint)dwForm.Handle; // IntPtr -> Int
			return DwCore.TryGetWindowById(id, out windowInfo);
		}

		public OSRect rect
        {
            get
            {
                var osRect = new OSRect
                {
                    Left    = Left,
                    Top     = Top,
                    Right   = Left + Width,
                    Bottom  = Top + Height,
                };
                return osRect;
            }
        }

        public int ScreenWidth => dwForm == null ? -1 : System.Windows.Forms.Screen.GetBounds(dwForm).Width;
        public int ScreenHeight => dwForm == null ? -1 : System.Windows.Forms.Screen.GetBounds(dwForm).Height;

        // For Drag Move
        private class DragDwFormInfo
        {
            public bool isDragging { get; private set; }
            public int offsetX;
            public int offsetY;
            public void StartDrag(DwForm from)
            {
                isDragging = true;

                var point = DwCore.GetOSCursorPos();
                offsetX = from.Left - point.x;
                offsetY = from.Top - point.y;
            }
            public void Reset()
            {
                isDragging = false;
                offsetX = 0;
                offsetY = 0;
            }
        }
        /// <summary>Drag control form <see cref="DwForm"/></summary>
        private DragDwFormInfo m_DragFormInfo = new DragDwFormInfo();
        public bool IsDragging => m_DragFormInfo?.isDragging ?? false;

        [SerializeField] private FormSetting m_Setting;
        public FormSetting setting
        {
            get
            {
                if (m_Setting == null)
					m_Setting = new FormSetting();
                return m_Setting;
			}
        }

        // Mascot Form Size
        private Vector2Int m_FormSizePre;

        // Update Function Type
        public UpdateFuncType UpdateFunc = UpdateFuncType.Update;

        // Chroma Key Compositing
        public bool ChromaKeyCompositing = false;
        public UnityEngine.Color ChromaKeyColor;
        public float ChromaKeyRange = 0.002f;
        private float chromaKeyRangePre;

        // RenderTexture
        private Material Mat_ReadBuffer;
        private Material Mat_Chromakey;

        // Rendering
        private Texture2D m_Texture;
		private DwForm _Form;
        public DwForm dwForm => _Form;

        private Camera m_LinkCamera = null;
        public Camera linkCamera
        { 
            get
            {
                if (m_LinkCamera == null)
                    m_LinkCamera = transform.GetComponent<Camera>();
                return m_LinkCamera;
            }
        }
        private Renderer m_Renderer;

		#region Mono
		/**
		private void OnValidate()
		{
			if (UnityEngine.Application.isPlaying || Time.timeSinceLevelLoad < 3f)
				return;

            if (linkCamera != null &&
                linkCamera.targetTexture is RenderTexture tex)
            {
                if (!tex.isReadable)
                {
                    Debug.LogError("Unable to adjust texture size, because it's not readable.");
				}
                else
                {
				    var size = setting.Size;
                    tex.width = size.x;
                    tex.height = size.y;
                }
			}
		}
		//**/

		private void Awake()
        {
            Debug.Log($"DwCamera Awake, App: {setting.id}");
		}

        private void Start()
		{
			using (new DwLogScope("DwCamera.Start", this))
			{
				linkCamera.clearFlags = CameraClearFlags.SolidColor;
				linkCamera.enabled = false; // for manually render to texture.

				m_Renderer = transform.GetComponent<Renderer>();

				Debug.Log("Prepare Shaders and read buffer", this);
				Mat_ReadBuffer = new Material(Shader.Find("DesktopWizard/Shaders/ReadBuffer"));
				Debug.Assert(Mat_ReadBuffer != null, "Fail to create ReadBuffer.", transform);

				Mat_Chromakey = new Material(Shader.Find("DesktopWizard/Shaders/Chromakey"));
				Debug.Assert(Mat_Chromakey != null, "Fail to create Chromakey.", transform);

				Debug.Log("Prepare DwFore setting", this);
				m_FormSizePre = setting.Size;
			}

			InitRenderTexture();
		}

        private void OnEnable()
        {
            InternalFormCreate();
		}

        private void OnDisable()
        {
            InternalUnlinkWindow();
            InternalFormDestory();
            DeinitGPU();
		}

        private void Update()
        {
            if (UpdateFunc == UpdateFuncType.Update)
                UpdateCore();
        }

        private void LateUpdate()
        {
            if (UpdateFunc == UpdateFuncType.LateUpdate)
                UpdateCore();

            InternalLinkWindow();
        }

		private void OnDrawGizmos()
        {
			rect.GizmosDraw(Color.green);
        }

        private void OnDestroy()
        {
            InternalFormDestory();

            if (m_Renderer?.sharedMaterial?.mainTexture != null)
                m_Renderer.sharedMaterial.mainTexture = null;
        }

        private void OnApplicationQuit()
        {
            DwCore.UnregisterClass("Mono.WinForms.1.0", IntPtr.Zero);
        }

		/****
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (ChromaKeyCompositing)
            {
                if (Mat_Chromakey != null)
                    UnityEngine.Graphics.Blit(source, destination, Mat_Chromakey);
            }
            else
            {
                if (Mat_ReadBuffer != null)
                    UnityEngine.Graphics.Blit(source, destination, Mat_ReadBuffer);
            }
        }
		//**/
        #endregion Mono

        #region Window Widget
        [Space]
        [Header("Effect")]
        [SerializeField] bool m_RemoveEffect = false;
		[SerializeField] private ComputeShader m_OutputShader;
		[SerializeField, Range(0, 1)] public float m_OpacityVal = 1;

		public bool m_GammaSpace = false;
		[Header("Shader - params")]
		[SerializeField, MinMaxSlider(0f, 8f)] public float m_Lod = 0f;
		[SerializeField] public bool m_Invert = false;

        [Header("Shader - FXAA")]
        public FXAAConfig m_FXAAConfig = new();
        [Serializable]
        public class FXAAConfig
		{
            public bool debug = false;

			public bool lowQuality = false;

			[Range(0.0312f, 0.0833f), Tooltip(
			"Trims the algorithm from processing darks.\r\n" +
            "  0.0833 - upper limit (default, the start of visible unfiltered edges)\r\n" +
            "  0.0625 - high quality (faster)\r\n" +
            "  0.0312 - visible limit (slower)")]
            public float contrastThreshold = 0.312f;

            [Range(0.063f, 0.333f), Tooltip(
			"The minimum amount of local contrast required to apply algorithm.\r\n" +
            "  0.333 - too little (faster)\r\n" +
            "  0.250 - low quality\r\n" +
            "  0.166 - default\r\n" +
            "  0.125 - high quality \r\n" +
            "  0.063 - overkill (slower)")]
			public float relativeThreshold = 0.063f;
			
            [Range(0f, 1f), Tooltip(
            "Choose the amount of sub-pixel aliasing removal.\r\n" +
            "This can effect sharpness.\r\n" +
            "  1.00 - upper limit (softer)\r\n" +
            "  0.75 - default amount of filtering\r\n" +
            "  0.50 - lower limit (sharper, less sub-pixel aliasing removal)\r\n" +
            "  0.25 - almost off\r\n" +
            "  0.00 - completely off")]
			public float subpixelBlending = 1f;

            [Range(0f, 1f)]
            public float weight = 1f;
        }

		[Header("Shader - boom")]
		public BoomConfig m_BoomConfig = new();
		[System.Serializable]
		public class BoomConfig
		{
            public bool debug = false;
			[Range(1, 16)] public int iteration = 1;
			[Range(0, 5)] public float intensity = 1f;
			[Range(0, 1)] public float bias = 1f;
			[Space]
			[Range(0, 2)] public float sampleOffset = 1f;
			[Range(0, 1)] public float threshold = 1f;
			[Range(0, 1)] public float softThreshold = 0.5f;
		}

		private class RawGPUWorker : GPUWorker
		{
			public RawGPUWorker(DwCamera dwc) : base(dwc) { }
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
				_camera.Render();
			}

			protected override void Dispose(bool disposing)
			{
                
				base.Dispose(disposing);
			}
		}
		private class ComputeGPUWorker : GPUWorker
		{
            public static readonly int inputTexId = Shader.PropertyToID("_InputTex");
			public static readonly int resultTexId = Shader.PropertyToID("_ResultTex");
			public static readonly int widthId = Shader.PropertyToID("_width");
			public static readonly int heightId = Shader.PropertyToID("_height");
            public static readonly int lodId = Shader.PropertyToID("_lod");
			public static readonly int invertId = Shader.PropertyToID("_invert");
            public static readonly int boomParams02Id = Shader.PropertyToID("_boomParams02");
            public static readonly int boomParams01Id = Shader.PropertyToID("_boomParams01");
            public static readonly int fxaaParams01Id = Shader.PropertyToID("_fxaaParams01");
			private RenderTexture uploadTexture = null;
			public readonly ComputeShader shader;
			public readonly int kernelIdx;
			
			public ComputeGPUWorker(DwCamera dwc, ComputeShader s) : base(dwc)
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
				_camera.Render();

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

		private abstract class GPUWorker : IDisposable
        {
            public readonly DwCamera dwc;
            public RenderTexture renderTexture = null;
			public int width    { get; protected set; } = -1;
			public int height   { get; protected set; } = -1;
			public bool IsDisposed { get; protected set; } = false;
			public GPUWorker(DwCamera dwc)
            {
                this.dwc = dwc;
			}
            public abstract void Execute(Renderer _renderer, Camera _camera, int _width, int _height);

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
				// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}
		}
        private GPUWorker m_RawGPU, m_GPU01, m_GPU02; // GPU01/02 Ping-Pong buffer
        private bool m_IsOdd = false;

        private Bitmap m_Bitmap = null;
        private void InitGPU()
        {
			using (new DwLogScope(nameof(InitGPU), this))
			{
				if (m_RawGPU == null || m_RawGPU.IsDisposed)
					m_RawGPU = new RawGPUWorker(this);

				if (m_OutputShader != null)
				{
					if ((m_GPU01 == null || m_GPU01.IsDisposed))
						m_GPU01 = new ComputeGPUWorker(this, m_OutputShader);

					if ((m_GPU02 == null || m_GPU02.IsDisposed))
						m_GPU02 = new ComputeGPUWorker(this, m_OutputShader);
				}
			}
		}
        private void DeinitGPU()
        {
            if (m_GPU01 != null)
                m_GPU01.Dispose();
            m_GPU01 = null;

			if (m_GPU02 != null)
				m_GPU02.Dispose();
			m_GPU02 = null;

			if (m_RawGPU != null)
                m_RawGPU.Dispose();
            m_RawGPU = null;

			if (m_Bitmap != null)
				m_Bitmap.Dispose();
			m_Bitmap = null;
        }

        private void InitRenderTexture()
        {
			using (new DwLogScope(nameof(InitRenderTexture), this))
			{
				ChromaKeyColor =
				linkCamera.backgroundColor =
				Mat_Chromakey.color = new UnityEngine.Color(ChromaKeyColor.r, ChromaKeyColor.g, ChromaKeyColor.b, 0.0f);

				ChromaKeyRange = Mathf.Clamp(ChromaKeyRange, 0.002f, 1.0f);
				chromaKeyRangePre = ChromaKeyRange;
				Mat_Chromakey.SetFloat("_Amount", chromaKeyRangePre);

				m_Renderer.material = ChromaKeyCompositing ? Mat_Chromakey : Mat_ReadBuffer;
				m_Texture = new Texture2D(setting.Size.x, setting.Size.y, TextureFormat.ARGB32, linear: false, mipChain: false, createUninitialized: true)
				{
					wrapMode = TextureWrapMode.Clamp,
					filterMode = FilterMode.Point,
					mipMapBias = 0f,
					anisoLevel = 0,
#if UNITY_EDITOR
					//https://issuetracker.unity3d.com/issues/texture2d-dot-alphaistransparency-causes-build-to-fail
					alphaIsTransparency = true,
#endif
					hideFlags = HideFlags.DontSave,
				};
			}
		}

		private int m_FirstUpdateCnt = 0;
		private void UpdateCore()
        {
			if (dwForm == null || !dwForm.Visible)
				return;

			var _first = m_FirstUpdateCnt == 0;

			// rendering
			_RenderToTexture();
			_HandleCoordInOS();

			// update form event within main thread.
			if (_first) Debug.Log("First - HandleWinFormEvents.", this);

			/// https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L544
			if (_Form == null)
			{
				Debug.LogWarning("First - Form instance not found.", this);
			}
			else
			{
				// read events from WinForm queues.
				if (_first) Debug.Log("First - Form instance found, process events.", this);
				_Form.ProcessEvents();
			}

			HandlerSelectedObjectEvents();
			CleanSubmitEvents();

			if (_first) Debug.Log("First - Update Core completed.", this);
			if (m_FirstUpdateCnt == 0)
			{
				++m_FirstUpdateCnt;
			}

			return;

            void _ChangeRenderTexture(Vector2Int size)
            {
                if (linkCamera == null || m_FormSizePre == size)
                    return;
				m_FormSizePre = size;
                m_Texture.Reinitialize(size.x, size.y, TextureFormat.ARGB32, false);
                dwForm.Size = new Size(size.x, size.y);
            }

            void _RenderToTexture()
            {
				if (dwForm != null && !dwForm.Visible || linkCamera == null)
				{
					if (_first) Debug.Log("First - RenderToTexture start Fail", this);
                    return;
				}
				if (_first) Debug.Log("First - RenderToTexture starting.", this);

				if (m_FormSizePre != setting.Size)
                {
                    _ChangeRenderTexture(setting.Size);
                }

                if (ChromaKeyColor.r != linkCamera.backgroundColor.r ||
                    ChromaKeyColor.g != linkCamera.backgroundColor.g ||
                    ChromaKeyColor.b != linkCamera.backgroundColor.b)
                {
                    ChromaKeyColor              = 
                    linkCamera.backgroundColor  = 
                    Mat_Chromakey.color         = new UnityEngine.Color(ChromaKeyColor.r, ChromaKeyColor.g, ChromaKeyColor.b, 0.0f);
                }

                if (ChromaKeyRange != chromaKeyRangePre)
                {
                    ChromaKeyRange = Mathf.Clamp(ChromaKeyRange, 0.002f, 1.0f);
                    chromaKeyRangePre = ChromaKeyRange;

                    if (Mat_Chromakey)
                        Mat_Chromakey.SetFloat("_Amount", chromaKeyRangePre);
				}

				if (setting.TopMost != dwForm.TopMost)
				{
					Debug.Log($"Form.TopMost changed: from {dwForm.TopMost} -> {setting.TopMost}");
					dwForm.TopMost = setting.TopMost;
				}

				if (_first) Debug.Log("First - RenderToTexture, start dump texture(s)", this);
				{
                    var isRaw   = m_RemoveEffect || m_OutputShader == null;
                    var gpu     = isRaw ? m_RawGPU : (m_IsOdd ? m_GPU01 : m_GPU02);
                    var prevSrc = isRaw ? m_RawGPU : (m_IsOdd ? m_GPU02 : m_GPU01);
                    if (!isRaw)
                        m_IsOdd = !m_IsOdd;

                    // use existing render texture.
                    if (gpu == null)
                    {
						// Allow hot plug output shader
                        InitGPU();
                        return; // skip this frame
                    }

                    if (prevSrc != null &&
						prevSrc.renderTexture != null &&
						prevSrc.width == setting.Size.x &&
						prevSrc.height == setting.Size.y)
                    {
						// Display last processed texture.
#if TRY_CATCH
						try
#endif
						{
							DwUtils.DumpTexture(prevSrc.renderTexture, m_Texture, Mat_Chromakey.color);
							DwUtils.DumpTexture(m_Texture, ref m_Bitmap);
							dwForm.Repaint(m_Bitmap, (byte)(Opacity * 255));
						}
#if TRY_CATCH
						catch (Exception ex)
						{
							Debug.LogException(ex);
						}
#endif
					}

                    // Capture current render into cache.
                    gpu.Execute(m_Renderer, linkCamera, setting.Size.x, setting.Size.y);
                }
				if (_first) Debug.Log("First - RenderToTexture dump texture success.", this);
            }

            void _HandleCoordInOS()
			{
				var allowDrag = setting == null ? false : setting.dragMethod != eDragMethod.None;
				if (allowDrag && m_DragFormInfo.isDragging)
                {
                    var cursor = DwCore.GetOSCursorPos();
					dwForm.Left = cursor.x + m_DragFormInfo.offsetX;
                    dwForm.Top = cursor.y + m_DragFormInfo.offsetY;
                }
				if (_first) Debug.Log("First - _HandleCoordInOS.", this);
            }
        }

		#endregion Window Widget

		#region Mouse Events
		private void AddEvents(DwForm f)
        {
            f.Event_MouseDown	+= Form_MouseDown;
            f.Event_MouseUp		+= Form_MouseUp;
            f.Event_MouseMove	+= Form_MouseMove;
            f.Event_MouseWheel	+= Form_MouseWheel;
            f.Event_KeyDown		+= Form_KeyDown;
            f.Event_KeyUp		+= Form_KeyUp;
            f.Event_GotFocus	+= Form_GotFocus;
            f.Event_LostFocus	+= Form_LostFocus;
			f.FormClosing       += Form_Closing;
        }

        private void RemoveEvents(DwForm f)
        {
            f.Event_MouseDown	-= Form_MouseDown;
            f.Event_MouseUp		-= Form_MouseUp;
            f.Event_MouseMove	-= Form_MouseMove;
            f.Event_MouseWheel	-= Form_MouseWheel;
            f.Event_KeyDown		-= Form_KeyDown;
            f.Event_KeyUp		-= Form_KeyUp;
            f.Event_GotFocus	-= Form_GotFocus;
            f.Event_LostFocus	-= Form_LostFocus;
			f.FormClosing       -= Form_Closing;
        }

        public delegate void PointerEventDelegate(PointerEventData evt);
        public event PointerEventDelegate
            EVENT_MouseMove, EVENT_MouseWheel,
            EVENT_MouseDown, EVENT_MouseUp;

        private bool IsMyForm(uint hWnd)
        {
			return dwForm == null ? false : hWnd == dwForm.hWnd;
		}

        private void Form_MouseDown(uint hWnd, PointerEventData pointerEvent)
		{
            if (!IsMyForm(hWnd))
                return;
			var _d = (m_Config.debug & eDebug.LogMouseEvent) != 0;
			if (_d) sb.Clear();
			var dragMethod = setting == null ? eDragMethod.None : setting.dragMethod;
			ProcessMouseEvent(ref pointerEvent);

			// https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L544
			// search for the control that will receive the press
			// if we can't find a press handler set the press
			// handler to be what would receive a click.
			var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;
			DeselectIfSelectionChanged(currentOverGo, pointerEvent);
			{
				pointerEvent.eligibleForClick = true;
				pointerEvent.delta = Vector2.zero;
				pointerEvent.dragging = false;
				pointerEvent.useDragThreshold = true; // Mouse Down
				pointerEvent.pointerPress = currentOverGo;
				pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
			}

			// search for the control that will receive the press
			// if we can't find a press handler set the press
			// handler to be what would receive a click.
			var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
			if (_d) sb.AppendLine($"Form_MouseDown - pointerDownHandler, first obj={currentOverGo}, take event={newPressed}");

			// didnt find a press handler... search for a click handler
			if (newPressed == null)
			{
				newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
				if (_d) sb.AppendLine($"Form_MouseDown - IPointerClickHandler, first obj={currentOverGo}, take event={newPressed}");
			}

			// Debug.Log("Pressed: " + newPressed);

			float time = Time.unscaledTime;

			if (newPressed == pointerEvent.lastPress)
			{
				var diffTime = time - pointerEvent.clickTime;
				if (diffTime < 0.3f)
					++pointerEvent.clickCount;
				else
					pointerEvent.clickCount = 1;

				pointerEvent.clickTime = time;
			}
			else
			{
				pointerEvent.clickCount = 1;
			}
			pointerEvent.pointerPress = newPressed;
			pointerEvent.rawPointerPress = currentOverGo;
			pointerEvent.clickTime = time;

			// Save the drag handler as well
			pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

			if (pointerEvent.pointerDrag != null)
			{
				ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
				if (_d) sb.AppendLine($"Form_MouseDown - initializePotentialDrag, drag obj={pointerEvent.pointerDrag}");
			}

			m_InputPointerEvent = pointerEvent; // Mouse Down


			switch (pointerEvent.button)
            {
                case InputButton.Left:
				{
				}
                break;
                case InputButton.Right:
                {
                    if (dragMethod == eDragMethod.HoldMouseRightBtn)
                        m_DragFormInfo.StartDrag(dwForm);
                }
                break;
                case InputButton.Middle:
                {
					if (dragMethod == eDragMethod.HoldMouseMiddleBtn)
						m_DragFormInfo.StartDrag(dwForm);
                }
                break;
            }

			if (sb.Length > 0)
				Debug.Log(sb.ToString(), this);
			EVENT_MouseDown?.TryCatchDispatchEventError(o => o?.Invoke(pointerEvent));
        }

		private void Form_MouseUp(uint hWnd, PointerEventData pointerEvent)
		{
			if (!IsMyForm(hWnd))
				return;
            var _d = (m_Config.debug & eDebug.LogMouseEvent) != 0;
			if (_d) sb.Clear();
			var dragMethod = setting == null ? eDragMethod.None : setting.dragMethod;
			ProcessMouseEvent(ref pointerEvent);
			var wasDragging = pointerEvent.dragging;
			{
				pointerEvent.eligibleForClick = true;
				pointerEvent.delta = Vector2.zero;
				pointerEvent.dragging = false;
				pointerEvent.useDragThreshold = false; // Mouse Up
			}

			if (wasDragging)
			{
				// Handle end drag outside of the dragging object
				var prev = m_InputPointerEvent;
				if (_d) sb.AppendLine($"Form_MouseUp - wasDragging, first obj={prev.pointerDrag.gameObject}");
				ReleaseMouse(pointerEvent, prev.pointerDrag.gameObject);
			}

			if (pointerEvent.pointerCurrentRaycast.gameObject != null)
			{
				var clickObj = pointerEvent.pointerCurrentRaycast.gameObject;
				var lastRelease = ExecuteEvents.ExecuteHierarchy(clickObj, pointerEvent, ExecuteEvents.pointerUpHandler);
				if (_d) sb.AppendLine($"Form_MouseUp - pointerUpHandler, first obj={clickObj}, take event={lastRelease}");
				ReleaseMouse(pointerEvent, pointerEvent.pointerCurrentRaycast.gameObject);
			}

			switch (pointerEvent.button)
            {
                case InputButton.Left: break;
                case InputButton.Middle:
                {
					if (dragMethod == eDragMethod.HoldMouseMiddleBtn)
						m_DragFormInfo.Reset();
				}
				break;
                case InputButton.Right:
				{
					if (dragMethod == eDragMethod.HoldMouseRightBtn)
                        m_DragFormInfo.Reset();
                }
                break;
			}
			if (sb.Length > 0)
				Debug.Log(sb.ToString(), this);

			EVENT_MouseUp?.TryCatchDispatchEventError(o => o?.Invoke(pointerEvent));
        }

		/// <summary>
		/// https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L186
		/// </summary>
		/// <param name="pointerEvent"></param>
		/// <param name="currentOverGo"></param>
		private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
        {
			var _d = (m_Config.debug & eDebug.LogMouseEvent) != 0;
			ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
			if (_d) sb.AppendLine($"ReleaseMouse pointerEvent={pointerEvent.pointerPress}, currentOverGo={currentOverGo}");
			var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
			// PointerClick and Drop events
			if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
			{
				ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
				if (_d) sb.AppendLine($"ReleaseMouse - pointerClickHandler, click obj={pointerEvent.pointerPress}");
			}
			else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
			{
				ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
				if (_d) sb.AppendLine($"ReleaseMouse - dropHandler, drop obj={currentOverGo}");
			}

			pointerEvent.eligibleForClick = false;
			pointerEvent.pointerPress = null;
			pointerEvent.rawPointerPress = null;

			if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
			{
				ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);
				if (_d) sb.AppendLine($"ReleaseMouse - endDragHandler, drag obj={pointerEvent.pointerDrag}");
			}

			pointerEvent.dragging = false;
			pointerEvent.pointerDrag = null;

			// redo pointer enter / exit to refresh state
			// so that if we moused over something that ignored it before
			// due to having pressed on something else
			// it now gets it.
			if (currentOverGo != pointerEvent.pointerEnter)
			{
				HandlePointerExitAndEnter(pointerEvent, null);
				HandlePointerExitAndEnter(pointerEvent, currentOverGo);
			}

			m_InputPointerEvent = pointerEvent; // Release / Mouse Up
		}

        private void Form_MouseMove(uint hWnd, PointerEventData pointerEvent)
		{
			if (!IsMyForm(hWnd))
				return;
			var _d = (m_Config.debug & eDebug.LogMouseEvent) != 0;
			if (_d) sb.Clear();
			ProcessMouseEvent(ref pointerEvent);

			// HandlerSelectedObjectEvents();

			/// <see cref="https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/PointerInputModule.cs#L330"/>
			if (!pointerEvent.dragging &&
				pointerEvent.pointerDrag != null &&
				ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
			{
				ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
				if (_d) sb.AppendLine($"Form_MouseMove - beginDragHandler, drag obj={pointerEvent.pointerDrag}");
				pointerEvent.dragging = true;
			}

			if (pointerEvent.dragging && pointerEvent.pointerDrag != null)
			{
				// Before doing drag we should cancel any pointer down state
				// And clear selection!
				if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
				{
					ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
					if (_d) sb.Append($"Form_MouseMove - pointerUpHandler. \n" +
						$"Press={pointerEvent.pointerPress}\n" +
						$"pointerDrag={pointerEvent.pointerDrag}\n");

					pointerEvent.eligibleForClick = false;
					pointerEvent.pointerPress = null;
					pointerEvent.rawPointerPress = null;
				}
				if (_d) sb.Append($"Form_MouseMove - dragHandler. pointerDrag={pointerEvent.pointerDrag}\n");
				ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
			}
			
			var hoverGo = pointerEvent.pointerCurrentRaycast.gameObject;
			HandlePointerExitAndEnter(pointerEvent, hoverGo);

			/**
			switch (pointerEvent.button)
            {
                case InputButton.Left: break;
				case InputButton.Middle: break;
				case InputButton.Right: break;
			}
			//**/

			m_InputPointerEvent = pointerEvent; // Mouse Move
			if (sb.Length > 0)
				Debug.Log(sb.ToString(), this);
			EVENT_MouseMove?.TryCatchDispatchEventError(o => o?.Invoke(pointerEvent));
		}

        private void Form_MouseWheel(uint hWnd, PointerEventData pointerEvent)
		{
			if (!IsMyForm(hWnd))
				return;
			ProcessMouseEvent(ref pointerEvent);
			var _d = (m_Config.debug & eDebug.LogMouseEvent) != 0;
			if (_d) sb.Clear();

			/// <see cref="https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L564C37-L564C82"/>
			var scrollDelta = pointerEvent.scrollDelta;
            if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
			{
                var ui = pointerEvent.pointerCurrentRaycast.gameObject;
				foreach(var r in s_RaycastResultList)
				{ 
					if (r.gameObject == ui)
						continue;
					var handler = r.gameObject.GetComponent<IEventSystemHandler>();
					if (handler is IScrollHandler scroll)
					{
						if (_d) sb.AppendLine($"Mouse Wheel[{pointerEvent.button}] scrollHandler, {scrollDelta:F2} {r.gameObject}");
						ExecuteEvents.ExecuteHierarchy(r.gameObject, pointerEvent, ExecuteEvents.scrollHandler);
						break;
					}
					else if (handler is IMoveHandler move)
					{
						var axisEventData = GetAxisEventData(scrollDelta.x, scrollDelta.y, 0.6f);
						if (_d) sb.AppendLine($"Mouse Wheel[{pointerEvent.button}] moveHandler, {scrollDelta:F2} {r.gameObject}");
						ExecuteEvents.ExecuteHierarchy(r.gameObject, axisEventData, ExecuteEvents.moveHandler);
						break;
					}
				}
			}


			if (sb.Length > 0)
				Debug.Log(sb.ToString(), this);
			EVENT_MouseWheel?.TryCatchDispatchEventError(o => o?.Invoke(pointerEvent));
		}
		#endregion Mouse Events

		#region Key Events
		public delegate void KeyUpEventDelegate(KeyUpEvent keyUpEvent);
		public event KeyUpEventDelegate EVENT_keyUp;
		public delegate void KeyDownEventDelegate(KeyDownEvent keyUpEvent);
		public event KeyDownEventDelegate EVENT_keyDown;

		private void Form_KeyUp(uint hWnd, Event e)
		{
			if (!IsMyForm(hWnd))
				return;
			KeyUpEvent evt = KeyUpEvent.GetPooled(e.character, e.keyCode, e.modifiers);
			var _d = (m_Config.debug & eDebug.LogKeyEvent) != 0;
			if (_d)
			{
				Debug.Log($"KeyUp {evt.keyCode}", this);
			}


			CacheSubmitEvents(evt);

			EVENT_keyUp?.TryCatchDispatchEventError(o => o?.Invoke(evt));
		}

		private void Form_KeyDown(uint hWnd, Event e)
		{
			if (!IsMyForm(hWnd))
				return;
			KeyDownEvent evt = KeyDownEvent.GetPooled(e.character, e.keyCode, e.modifiers);
			var _d = (m_Config.debug & eDebug.LogKeyEvent) != 0;
			if (_d)
			{
				Debug.Log($"KeyDn {evt.keyCode}", this);
			}
			if (EventSystem.current?.currentSelectedGameObject != null)
            {
				var baseEvt = new BaseEventData(EventSystem.current);
                var go = EventSystem.current.currentSelectedGameObject;

				if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
					ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, baseEvt, ExecuteEvents.submitHandler);

				if (e.keyCode == KeyCode.Escape)
					ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, baseEvt, ExecuteEvents.cancelHandler);
			}
            EVENT_keyDown?.TryCatchDispatchEventError(o => o?.Invoke(evt));
        }
        #endregion Key Events

        #region Focus
        public delegate void GotFocusDelegate();
        public event GotFocusDelegate EVENT_GotFocus;
        private void Form_GotFocus(uint hWnd, EventArgs evt)
		{
			if (!IsMyForm(hWnd))
				return;
			CancelAllDragging();
            EVENT_GotFocus?.TryCatchDispatchEventError(o => o?.Invoke());
		}
		public delegate void LoseFocusDelegate();
		public event LoseFocusDelegate EVENT_LostFocus;
        private void Form_LostFocus(uint hWnd, EventArgs evt)
		{
			if (!IsMyForm(hWnd))
				return;
			CancelAllDragging();
			EVENT_LostFocus?.TryCatchDispatchEventError(o => o?.Invoke());
        }
        private void CancelAllDragging()
		{
			/// <see cref="https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L172"/>
			if (m_InputPointerEvent != null)
			{
				ReleaseMouse(m_InputPointerEvent, m_InputPointerEvent.pointerCurrentRaycast.gameObject);
			}
			m_InputPointerEvent = null;
			if (m_DragFormInfo.isDragging)
				m_DragFormInfo.Reset();
		}
		#endregion Focus

		#region DwForm state handle
		public event System.Action EVENT_Closed;
		private void Form_Closing(object sender, EventArgs e)
        {
            this.gameObject.SetActive(false);
			InternalFormDestory();
        }

        private void InternalFormCreate()
        {
			using (new DwLogScope($"DwForm {gameObject.name} FormCreate", this))
			{
				if (dwForm != null)
					throw new System.Exception("Form duplicate.");
				_Form = new DwForm(this);

				Debug.Log($"Form created: {gameObject.name}, add events.", this);
				AddEvents(dwForm);
				dwForm.Show();
			}
		}
        private void InternalFormDestory()
        {
            if (dwForm != null)
            {
				EVENT_Closed.TryCatchDispatchEventError(o => o?.Invoke());
				CancelAllDragging();
				RemoveEvents(dwForm);
                dwForm.Close();
            }
            _Form = null;
        }
        #endregion DwForm state handle

        #region Monitor Space Body

        [Header("Delay binding")]
        [SerializeField, ReadOnly] DwWindow m_Window = null;
        public DwWindow dwWindow => m_Window;
        private void InternalLinkWindow()
        {
            if (this.dwWindow != null)
                return;

            if (!TryGetWindow(out var wnd))
                return;
            if (wnd == null)
                throw new System.Exception("Logic error.");

            this.m_Window = wnd;
            this.m_Window.LinkCamera(this);
		}
        private void InternalUnlinkWindow()
        {
            if (this.m_Window != null)
                this.m_Window.LinkCamera(null);
            this.m_Window = null;
        }

        /// <summary>Try get window via DwOSBody</summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public bool TryGetWindow(out DwWindow window)
        {
            window = default;
            if (!DwCore.instance.TryGetOS(out var os))
                return false;
            var id = (uint)dwForm.Handle;
            return os.TryGetWindowById(id, out window);
        }

		#endregion Monitor Space Body

		#region Matrix Transform

		/// <summary>
		/// Convert Form's  space (world space which depend on Window Form coordinate)
		/// into current Form's 2D screen space (render textures size)
		/// </summary>
		/// <returns></returns>
		internal static readonly Matrix4x4 s_QuickOSToMonitor = Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
		public Matrix4x4 MatrixOSToMonitor()        => s_QuickOSToMonitor;
		public Matrix4x4 MatrixMonitorToOS()        => s_QuickOSToMonitor.inverse;

        public Matrix4x4 MatrixMonitorToForm()      => dwForm?.MatrixMonitorToForm() ?? Matrix4x4.Translate(new Vector3(-Left, + Top + Height, 0));

		public Matrix4x4 MatrixFormToMonitor()      => dwForm?.MatrixFormToMonitor() ?? MatrixMonitorToForm().inverse;

		public Matrix4x4 MatrixOSToForm()           => MatrixMonitorToForm() * MatrixOSToMonitor(); // order matter.
        public Matrix4x4 MatrixFormToOS()           => MatrixOSToForm().inverse;

        public Vector3 MonitorToFormVector(Vector3 world)
        {
            world.z = 0f;
            return MatrixFormToMonitor().inverse.MultiplyVector(world);
        }

        public Vector3 MonitorToFormPoint(Vector3 world)
		{
			world.z = 0f;
			return MatrixFormToMonitor().inverse.MultiplyPoint3x4(world);
		}

		public Vector3 FormToMonitorVector(Vector3 local)
        {
            local.z = 0f;
			return MatrixFormToMonitor().MultiplyVector(local);
        }
        public Vector3 FormToMonitorPoint(Vector3 local)
		{
			local.z = 0f;
			return MatrixFormToMonitor().MultiplyPoint3x4(local);
		}
        
        // Tested
        public Ray FormToModelRay(Vector3 formPoint)
        {
            formPoint.z = 0f;
            var r = linkCamera.ScreenPointToRay(formPoint, Camera.MonoOrStereoscopicEye.Mono);
            return r;
        }

        // Tested
		public Vector3 FormToModelPoint(Vector3 formPoint, float zOverride = -1f)
		{
            var r = FormToModelRay(formPoint);
            var dis = zOverride >= 0f ? zOverride : linkCamera.farClipPlane;
			return r.origin + r.direction.normalized * dis;
		}

        // Tested
        public Vector3 ModelToFormPoint(Vector3 modelPoint)
        {
            var monPos = linkCamera.WorldToScreenPoint(modelPoint);
            monPos.z = 0f;
            return monPos;
        }

        // Tested
        public Vector3 ModelToMonitorPoint(Vector3 modelPoint)
        {
            var formPos = ModelToFormPoint(modelPoint);
            var f2m = MatrixFormToMonitor().MultiplyPoint3x4(formPos);
            return f2m;
        }

        public Vector3 MonitorToModelPoint(Vector3 monPoint, float zOverride = -1f)
        {
            var formPos = MatrixMonitorToForm().MultiplyPoint3x4(monPoint);
			var r = linkCamera.ScreenPointToRay(formPos, Camera.MonoOrStereoscopicEye.Mono);
			var dis = zOverride >= 0f ? zOverride : linkCamera.farClipPlane;
			return r.origin + r.direction.normalized * dis;
		}

		#endregion Matrix Transform

		#region Mouse
		/// <summary>Mouse pos in OS space</summary>
		/// <returns></returns>
		public Vector2Int GetMousePosInOSSpace() => DwCore.GetOSCursorPos();

        /// <summary>The mouse position in the monitor space.</summary>
        /// <returns></returns>
		public Vector2 GetMousePosInMonitorSpace()
		{
			//// U3D's y-axis is flipped, upward = y++, downward = y--
			//// Window's y-axis is, upward = y--, downward = y++
			var v2i = DwCore.GetOSCursorPos();
			var osV3f = new Vector3(v2i.x, v2i.y, 0f);
			var world = MatrixOSToMonitor().MultiplyPoint3x4(osV3f);
            return new Vector2(world.x, world.y);
		}

		/// <summary>
		/// The mouse position in the form space.
		/// on <see cref="DwCamera"/>'s near clip plane.
		/// </summary>
		/// <returns></returns>
		public Vector2 GetMousePosInFormSpace()
		{
			var v2i = DwCore.GetOSCursorPos();
			var v3f = new Vector3(v2i.x, v2i.y, 0f);
			var p = MatrixOSToForm().MultiplyPoint3x4(v3f);
			return new Vector2(p.x, p.y);
		}

		/// <summary>
		/// Model Space is a space that is relative to the camera's position.
		/// </summary>
		/// <returns></returns>
		public Ray GetMouseRayInModelSpace()
        {
			var camPos = (Vector3)GetMousePosInFormSpace();
            var ray = m_LinkCamera.ScreenPointToRay(camPos, Camera.MonoOrStereoscopicEye.Mono);
            return ray;
		}

		#endregion Mouse

		#region Convert Form input events to U3D events
		private EventSystem eventSystem => EventSystem.current;
		private int m_ConsecutiveMoveCount = 0;
		private PointerEventData m_InputPointerEvent;

		// https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L277
		private void HandlerSelectedObjectEvents()
		{
			if (_Form == null || !_Form.Focused)
				return;

			// not sure if this is needed, since we already set the selected object in Form_MouseDown.
			// https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L252
			//var toSelect = eventSystem.currentSelectedGameObject;
			//if (toSelect == null)
			//	toSelect = eventSystem.firstSelectedGameObject;
			//eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());

			if (eventSystem.currentSelectedGameObject == null)
				return;

			bool usedEvent = SendUpdateEventToSelectedObject();
			if (eventSystem.sendNavigationEvents)
			{
				if (!usedEvent)
					usedEvent |= SendMoveEventToSelectedObject();

				if (!usedEvent)
					SendSubmitEventToSelectedObject();
			}
		}

		protected bool SendUpdateEventToSelectedObject()
		{
			if (eventSystem.currentSelectedGameObject == null)
				return false;

			var go = eventSystem.currentSelectedGameObject;
			var data = GetBaseEventData();
			if (!ExecuteEvents.Execute(go, data, ExecuteEvents.updateSelectedHandler))
				return true; // no handler found.

			var _d = (m_Config.debug & eDebug.LogUIEvents) != 0;
			if (_d) sb.AppendLine($"SendMoveEventToSelectedObject - updateSelectedHandler, go={go}");

			return data.used;
		}

		/// <summary>https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L66</summary>
		const float s_InputActionsPerSecond = 10f;
		const float s_RepeatDelay = 0.5f;
		private float m_PrevActionTime;
		private Vector2 m_LastMoveVector;
		/// <summary>
		/// Calculate and send a move event to the current selected object.
		/// </summary>
		/// <returns>If the move event was used by the selected object.</returns>
		protected bool SendMoveEventToSelectedObject()
		{
			float time = Time.unscaledTime;

			/// Note : look like it's used for Joystick.
			/// <see cref="GetRawMoveVector()"/>
			/// <seealso cref="https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L458"/>
			Vector2 movement = GetRawMoveVector();
			if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
			{
				m_ConsecutiveMoveCount = 0;
				return false;
			}

			bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);

			// If direction didn't change at least 90 degrees, wait for delay before allowing consequtive event.
			if (similarDir && m_ConsecutiveMoveCount == 1)
			{
				if (time <= m_PrevActionTime + s_RepeatDelay)
					return false;
			}
			// If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
			else
			{
				if (time <= m_PrevActionTime + 1f / s_InputActionsPerSecond)
					return false;
			}

			var axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);

			if (eventSystem.currentSelectedGameObject && axisEventData.moveDir != MoveDirection.None)
			{
				var go = eventSystem.currentSelectedGameObject;
				ExecuteEvents.Execute(go, axisEventData, ExecuteEvents.moveHandler);

				var _d = (m_Config.debug & eDebug.LogUIEvents) != 0;
				if (_d) sb.AppendLine($"SendMoveEventToSelectedObject - moveHandler, go={go}, axisEventData={axisEventData}");

				if (!similarDir)
					m_ConsecutiveMoveCount = 0;
				m_ConsecutiveMoveCount++;
				m_PrevActionTime = time;
				m_LastMoveVector = movement;
			}
			else
			{
				m_ConsecutiveMoveCount = 0;
			}

			return axisEventData.used;
		}


		/// <summary>
		/// Calculate and send a submit event to the current selected object.
		/// </summary>
		/// <returns>If the submit event was used by the selected object.</returns>
		protected bool SendSubmitEventToSelectedObject()
		{
			if (eventSystem.currentSelectedGameObject == null)
				return false;
			var _d = (m_Config.debug & eDebug.LogUIEvents) != 0;
			var data = GetBaseEventData();
			while (m_SubmitEvent.Count > 0)
			{
				var evt = m_SubmitEvent.Dequeue();
				switch (evt.type)
				{
					// 0 = Cancel
					case 0:
					if (_d) Debug.Log("SendSubmitEventToSelectedObject, cancelHandler");
					ExecuteEvents.Execute(evt.gameobject, data, ExecuteEvents.cancelHandler);
					break;
					// 1 = Submit
					case 1:
					if (_d) Debug.Log("SendSubmitEventToSelectedObject, submitHandler");
					ExecuteEvents.Execute(evt.gameobject, data, ExecuteEvents.submitHandler);
					break;
					default: break;
				}
			}
			return data.used;
		}
		private void CacheSubmitEvents(KeyUpEvent evt)
		{
			if (!eventSystem.sendNavigationEvents)
				return;
			if (eventSystem.currentSelectedGameObject == null)
				return;
			var go = eventSystem.currentSelectedGameObject;
			var isSubmit = evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return;
			if (isSubmit)
				m_SubmitEvent.Enqueue(new SubmitInfo(go, 1));
			var isCancel = evt.keyCode == KeyCode.Escape;
			if (isCancel)
				m_SubmitEvent.Enqueue(new SubmitInfo(go, 0));
		}
		private void CleanSubmitEvents()
		{
			m_SubmitEvent.Clear();
		}

		private struct SubmitInfo
		{
			public GameObject gameobject;
			public int type; // 0 = cancel, 1 = submit
			public SubmitInfo(GameObject gameObject, int type)
			{
				this.gameobject = gameObject;
				this.type = type;
			}
		}
		private Queue<SubmitInfo> m_SubmitEvent = new Queue<SubmitInfo>(4);
		
		private AxisEventData GetAxisEventData(float x, float y, float deadZone)
		{
			var axisEventData = new AxisEventData(EventSystem.current);

			axisEventData.Reset();
			axisEventData.moveVector = new Vector2(x, y);
			axisEventData.moveDir = MoveDirection.None;
			if (Mathf.Abs(x) > deadZone)
				axisEventData.moveDir = (x > 0) ? MoveDirection.Right : MoveDirection.Left;
			else if (Mathf.Abs(y) > deadZone)
				axisEventData.moveDir = (y > 0) ? MoveDirection.Up : MoveDirection.Down;
			return axisEventData;
		}
		protected GameObject GetCurrentFocusedGameObject() => eventSystem.currentSelectedGameObject;

		/// <summary>
		/// https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/PointerInputModule.cs#L405
		/// </summary>
		/// <param name="currentOverGo"></param>
		/// <param name="pointerEvent"></param>
		protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
		{
			// Selection tracking
			var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
			// if we have clicked something new, deselect the old thing
			// leave 'selection handling' up to the press event though.
			if (selectHandlerGO != eventSystem.currentSelectedGameObject)
			{
				eventSystem.SetSelectedGameObject(null, pointerEvent);
				var _d = (m_Config.debug & eDebug.LogUIEvents) != 0;
				if (_d) Debug.Log($"DeselectIfSelectionChanged, ISelectHandler = {selectHandlerGO}");
			}
		}
		// walk up the tree till a common root between the last entered and the current entered is foung
		// send exit events up to (but not inluding) the common root. Then send enter events up to
		// (but not including the common root).
		protected void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget)
		{
			// if we have no target / pointerEnter has been deleted
			// just send exit events to anything we are tracking
			// then exit
			if (newEnterTarget == null || currentPointerData.pointerEnter == null)
			{
				for (var i = 0; i < currentPointerData.hovered.Count; ++i)
					ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerExitHandler);

				currentPointerData.hovered.Clear();

				if (newEnterTarget == null)
				{
					currentPointerData.pointerEnter = null;
					return;
				}
			}

			// if we have not changed hover target
			if (currentPointerData.pointerEnter == newEnterTarget && newEnterTarget)
				return;

			GameObject commonRoot = FindCommonRoot(currentPointerData.pointerEnter, newEnterTarget);

			// and we already an entered object from last time
			if (currentPointerData.pointerEnter != null)
			{
				// send exit handler call to all elements in the chain
				// until we reach the new target, or null!
				Transform t = currentPointerData.pointerEnter.transform;

				while (t != null)
				{
					// if we reach the common root break out!
					if (commonRoot != null && commonRoot.transform == t)
						break;

					ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
					currentPointerData.hovered.Remove(t.gameObject);
					t = t.parent;
				}
			}

			// now issue the enter call up to but not including the common root
			currentPointerData.pointerEnter = newEnterTarget;
			if (newEnterTarget != null)
			{
				Transform t = newEnterTarget.transform;

				while (t != null && t.gameObject != commonRoot)
				{
					ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerEnterHandler);
					currentPointerData.hovered.Add(t.gameObject);
					t = t.parent;
				}
			}
		}
		/// <summary>
		/// Given 2 GameObjects, return a common root GameObject (or null).
		/// </summary>
		/// <param name="g1">GameObject to compare</param>
		/// <param name="g2">GameObject to compare</param>
		/// <returns></returns>
		protected static GameObject FindCommonRoot(GameObject g1, GameObject g2)
		{
			if (g1 == null || g2 == null)
				return null;

			var t1 = g1.transform;
			while (t1 != null)
			{
				var t2 = g2.transform;
				while (t2 != null)
				{
					if (t1 == t2)
						return t1.gameObject;
					t2 = t2.parent;
				}
				t1 = t1.parent;
			}
			return null;
		}
		private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
		{
			if (!useDragThreshold)
				return true;

			return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
		}


		private static List<RaycastResult> s_RaycastResultList = null;

		/// <see cref="EventSystem.RaycastComparer(RaycastResult, RaycastResult)"/>
		private static int s_RaycastComparer(RaycastResult lhs, RaycastResult rhs)
		{
			if (lhs.module != rhs.module)
			{
				var lhsEventCamera = lhs.module.eventCamera;
				var rhsEventCamera = rhs.module.eventCamera;
				if (lhsEventCamera != null && rhsEventCamera != null && lhsEventCamera.depth != rhsEventCamera.depth)
				{
					// need to reverse the standard compareTo
					if (lhsEventCamera.depth < rhsEventCamera.depth)
						return 1;
					if (lhsEventCamera.depth == rhsEventCamera.depth)
						return 0;

					return -1;
				}

				if (lhs.module.sortOrderPriority != rhs.module.sortOrderPriority)
					return rhs.module.sortOrderPriority.CompareTo(lhs.module.sortOrderPriority);

				if (lhs.module.renderOrderPriority != rhs.module.renderOrderPriority)
					return rhs.module.renderOrderPriority.CompareTo(lhs.module.renderOrderPriority);
			}

			// Renderer sorting
			if (lhs.sortingLayer != rhs.sortingLayer)
			{
				// Uses the layer value to properly compare the relative order of the layers.
				var rid = SortingLayer.GetLayerValueFromID(rhs.sortingLayer);
				var lid = SortingLayer.GetLayerValueFromID(lhs.sortingLayer);
				return rid.CompareTo(lid);
			}

			if (lhs.sortingOrder != rhs.sortingOrder)
				return rhs.sortingOrder.CompareTo(lhs.sortingOrder);

			// comparing depth only makes sense if the two raycast results have the same root canvas (case 912396)
			if (lhs.depth != rhs.depth && lhs.module.rootRaycaster == rhs.module.rootRaycaster)
				return rhs.depth.CompareTo(lhs.depth);

			if (lhs.distance != rhs.distance)
				return lhs.distance.CompareTo(rhs.distance);

#if PACKAGE_PHYSICS2D
			// Sorting group
            if (lhs.sortingGroupID != SortingGroup.invalidSortingGroupID && rhs.sortingGroupID != SortingGroup.invalidSortingGroupID)
            {
                if (lhs.sortingGroupID != rhs.sortingGroupID)
                    return lhs.sortingGroupID.CompareTo(rhs.sortingGroupID);
                if (lhs.sortingGroupOrder != rhs.sortingGroupOrder)
                    return rhs.sortingGroupOrder.CompareTo(lhs.sortingGroupOrder);
            }
#endif

			return lhs.index.CompareTo(rhs.index);
		}

		private void FindRaycastObjInOrder(PointerEventData evt)
		{
			if (s_RaycastResultList == null)
				s_RaycastResultList = new List<RaycastResult>(32);
			var cam = this.linkCamera;
			if (cam == null)
				throw new System.NullReferenceException();
			s_RaycastResultList.Clear();
			var modules = RaycasterManager.GetRaycasters();
			for (int i = 0; i < modules.Count; ++i)
			{
				var raycaster = modules[i];
				if (raycaster == null || !raycaster.IsActive())
					continue;
				if (cam != raycaster.eventCamera)
					continue;

				raycaster.Raycast(evt, s_RaycastResultList);
			}
			s_RaycastResultList.Sort(s_RaycastComparer);
			// return s_RaycastResultList.ToArray();
		}

		private System.Text.StringBuilder sb = new System.Text.StringBuilder();

		/// <summary>
		/// Return the first valid RaycastResult.
		/// </summary>
		protected static RaycastResult FindFirstRaycast()
		{
			for (var i = 0; i < s_RaycastResultList.Count; ++i)
			{
				if (s_RaycastResultList[i].gameObject == null)
					continue;

				return s_RaycastResultList[i];
			}
			return new RaycastResult();
		}

		private bool m_PassFirstMouseEvent = false;
		private Vector2 m_LastMousePosition;
		private Vector2 m_MousePosition;
		/// <summary>
		/// Cache mouse position delta, position, vector.
		/// </summary>
		/// <param name="raycastResult"></param>
		private void ProcessMouseEvent(ref PointerEventData pointerEvent)
		{
			FindRaycastObjInOrder(pointerEvent);
			var raycastResult = FindFirstRaycast();
			if (!raycastResult.isValid)
			{
				// ISSUE: pointerCurrentRaycast is invalid(empty), when no UGUI was found.
				// in DwOS, we want the mouse screen position even if no UGUI was found.
				// for example, do math calculation, and project the raycast into the model space.
				// Solution :
				// we craft a new raycast result based on mouse position in the monitor space.
				var monPos = GetMousePosInMonitorSpace();

				raycastResult = new RaycastResult
				{
					screenPosition = monPos,
				};

				if ((pointerEvent.pressPosition - pointerEvent.position).sqrMagnitude < 0.001f)
					pointerEvent.pressPosition = monPos;

				pointerEvent.position = monPos;
			}

			if (!m_PassFirstMouseEvent)
			{
				m_MousePosition = raycastResult.screenPosition;
				m_PassFirstMouseEvent = true;
			}
			m_LastMousePosition = m_MousePosition;
			m_MousePosition = raycastResult.screenPosition;
			m_RawMoveVector = m_MousePosition - m_LastMousePosition;

			if (m_InputPointerEvent != null)
			{
				var o = m_InputPointerEvent;
				pointerEvent.pointerDrag		= o.pointerDrag;
				pointerEvent.dragging			= o.dragging;
				pointerEvent.useDragThreshold	= o.useDragThreshold;
				pointerEvent.pointerPress		= o.pointerPress;
				pointerEvent.pointerPressRaycast = o.pointerPressRaycast;
				pointerEvent.rawPointerPress = o.rawPointerPress;
			}

			pointerEvent.pointerCurrentRaycast = raycastResult;
			pointerEvent.delta = m_RawMoveVector;
		}


		Vector2 m_RawMoveVector = default;
		private Vector2 GetRawMoveVector()
		{
			/// Org code <see cref="https://github.com/Unity-Technologies/uGUI/blob/5ab4c0fee7cd5b3267672d877ec4051da525913c/UnityEngine.UI/EventSystem/InputModules/StandaloneInputModule.cs#L458"/>
			/// since we don't need to care about joystick.
			return m_RawMoveVector;
		}

		private BaseEventData m_BaseEventData = null;
		private BaseEventData GetBaseEventData()
		{
			if (m_BaseEventData == null)
				m_BaseEventData = new BaseEventData(EventSystem.current);
			return m_BaseEventData;
		}
		#endregion Convert Form input events to U3D events

		#region Win Raycast
		public bool RaycastBorderWin(Vector2 screenPosition, Vector2 directionNoZAxis, float maxDistance, out MonitorRaycastResult rst)
		{
			return Raycast(screenPosition, directionNoZAxis, maxDistance, out rst,
				DwCore.layer.BorderLayerMask | DwCore.layer.WindowLayerMask,
				QueryTriggerInteraction.Collide);
		}

		public bool Raycast(Vector2 screenPosition, Vector2 directionNoZAxis, float maxDistance, out MonitorRaycastResult rst,
			int layerMask = Physics.AllLayers,
			QueryTriggerInteraction qti = QueryTriggerInteraction.UseGlobal)
		{
			rst = new MonitorRaycastResult
			{
				orgin = screenPosition,
				direction = directionNoZAxis,
				distance = maxDistance,
				endPoint = directionNoZAxis.normalized * maxDistance + screenPosition,
			};
			rst.Fetch(this);

			var p0 = (Vector3)screenPosition;
			var dir = (Vector3)directionNoZAxis;
			if (!Physics.Raycast(p0, dir, out var hitinfo, maxDistance, layerMask, qti))
			{
				return false;
			}


			rst.collider = hitinfo.collider;
			rst.distance = hitinfo.distance;
			rst.endPoint = hitinfo.point;

			Debug.Assert(hitinfo.collider, "Collider not found.", this);
			rst.isBorder = rst.collider.gameObject.layer == DwCore.layer.BorderLayer;
			rst.isWindow = rst.collider.gameObject.layer == DwCore.layer.WindowLayer;

			if (rst.isWindow)
			{
				rst.window = rst.collider.gameObject.GetComponent<DwWindow>();
				Debug.Assert(rst.window, "Window not found.", this);
				rst.window.DebugDraw(Color.magenta);
			}

			return true;
		}
		#endregion Win Raycast

	}
}