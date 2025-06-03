using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
using System;
using Newtonsoft.Json;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace Gaia
{
	/// <summary>
	/// A set of game quality settings.
    /// allow to 
	/// </summary>
	public partial class GameQuality
    {
        #region Singleton
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void CreateSingletonOnLaunch()
        {
            ReferenceEquals(Instance, null);
        }

        public static GameQuality InstanceWithoutCreate => m_Instance;

		private static GameQuality m_Instance = null;
        public static GameQuality Instance
		{
			get
			{
				if (m_Instance == null)
				{
					_ = new GameQuality();
				}
				return m_Instance;
			}
		}

        private GameQuality()
        {
            if (m_Instance != null)
			{
				var ex = new Exception("Double instance.");
				Debug.LogException(ex);
				return;
			}
			m_Instance = this;
            Init();
		}

		#endregion Singleton

        public bool IsInited = false;
        private void Init()
        {
			Debug.Log($"GameQuality init.... instance={InstanceWithoutCreate}");
            
            if (IsInited)
            {
                var ex = new Exception("Double init.");
                Debug.LogException(ex);
                return;
            }
			foreach (var o in Instance.GetOptions())
			{
				if (o == null)
					continue;
				o.ResetPreview();
			}

            Debug.Log("GameQuality init completed.");
			TriggerUpdate();
			IsInited = true;
		}

		#region Require Implement
		private const string s_Prefix = "Gx_";

        public static event System.Action EVENT_Updated;
        private static void TriggerUpdate() => EVENT_Updated.TryCatchDispatchEventError(o => o?.Invoke());

		/// <summary>
		/// Check all options on list, if any of them is dirty,
        /// show a popup to ask user to keep or revert the changes.
		/// </summary>
		/// <param name="isDirty"></param>
		internal static void ConfirmApplySetting(out bool isDirty)
        {
            bool m_Dirty = false;
            foreach (var o in Instance.GetOptions())
            {
                if (o == null)
                    continue;
                m_Dirty |= o.IsDirty;
            }
            isDirty = m_Dirty;
            if (isDirty)
            {
                TriggerUpdate();
                //AxPopup.ShowResolutionChanged(OnKeepChangesButtonClicked, OnRevertChangesButtonClicked);
            }
        }

        private static void OnKeepChangesButtonClicked()
        {
            Debug.LogWarning("[ShowResolutionChanged] : Apply");
            foreach (var o in Instance.GetOptions())
            {
                o.Save();
            }
            TriggerUpdate();
        }

        private static void OnRevertChangesButtonClicked()
        {
            Debug.LogWarning("[ShowResolutionChanged] : Revert");
            foreach (var o in Instance.GetOptions())
            {
                o.ResetPreview();
            }
            TriggerUpdate();
        }
        #endregion Require Implement

        #region Options
        private GxSettingBase[] m_Options = null;

        public IEnumerable<GxSettingBase> GetOptions()
        {
			/// on demend collect options into cache.
			if (m_Options == null)
            {
                if (InstanceWithoutCreate == null)
                    throw new NullReferenceException();
                var type = GetType();
			    var targetType = typeof(GxSettingBase);
			    var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                var pList = new List<GxSettingBase>();
                foreach (var property in properties)
                {
				    if (!property.CanRead)
					    continue;
				    if (!targetType.IsAssignableFrom(property.PropertyType))
					    continue;
                    try
                    {
                        var obj = property.GetValue(this);
                        var getter = obj as GxSettingBase;
                        pList.Add(getter);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"{property.Name}\n{ex.InnerException}");
                    }
			    }
                m_Options = pList.ToArray();
            }
            
            foreach (var o in m_Options)
                yield return o;
		}
		#endregion Options

		#region Graphic Setting
		public enum GraphicSettingBehaviour
		{
			Low,
			Medium,
			High,
			Custom,
		}

        private GxSetting<GraphicSettingBehaviour> m_GraphicSetting;
		public GxSetting<GraphicSettingBehaviour> GraphicSetting
        {
            get
            {
                if (m_GraphicSetting == null)
                {
#if UNITY_IPHONE || UNITY_ANDROID
                    var _defaultSetting = GraphicSettingBehaviour.Low;
#else
					var _defaultSetting = GraphicSettingBehaviour.High;
#endif
                    m_GraphicSetting = new GxSetting<GraphicSettingBehaviour>(
						s_Prefix + nameof(GraphicSetting),
                        _defaultSetting,
						_OnSettingUpdate
					);
                    _OnSettingUpdate(_defaultSetting);
				}
                return m_GraphicSetting;

                void _OnSettingUpdate(GraphicSettingBehaviour data)
                {
                    TriggerUpdate();
                }
            }
        }
		#endregion Graphic Setting

		#region RenderScale
		public enum RenderScaleBehaviour
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Ultra = 3,
        }
        private GxSetting<RenderScaleBehaviour> m_RenderScale;
        public GxSetting<RenderScaleBehaviour> RenderScale
        {
            get
            {
                if (m_RenderScale == null)
                {
#if UNITY_IPHONE || UNITY_ANDROID
					var _defaultSetting = RenderScaleBehaviour.Low;
#else
					var _defaultSetting = RenderScaleBehaviour.Ultra;
#endif
                    m_RenderScale = new GxSetting<RenderScaleBehaviour>(
						s_Prefix + nameof(RenderScale),
                        _defaultSetting,
                        _OnSettingUpdate
                    );
					_OnSettingUpdate(_defaultSetting);
				}
                return m_RenderScale;

                void _OnSettingUpdate(RenderScaleBehaviour data)
				{
					var urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
                    if (urpAsset == null)
                        return;

                    var value = data switch
                    {
                        RenderScaleBehaviour.Low    => 0.1f,
                        RenderScaleBehaviour.Medium => 0.5f,
                        RenderScaleBehaviour.High   => 0.75f,
                        RenderScaleBehaviour.Ultra  => 1f,
                        _ => 0.1f
                    };

					urpAsset.renderScale = value;
					TriggerUpdate();
				}
            }
        }
		#endregion RenderScale

		#region Target Frame Rate
		public enum eTargetFrameRateBehaviour : int
        {
            FPS30 = 30,
            FPS60 = 60,
            FPS90 = 90,
        }

        private GxSetting<eTargetFrameRateBehaviour> m_TargetFrameRate;
        public GxSetting<eTargetFrameRateBehaviour> TargetFrameRate
        {
            get
            {
                if (m_TargetFrameRate == null)
                {
#if UNITY_IPHONE || UNITY_ANDROID
                    var _defaultSetting = TargetFrameRateBehaviour.Low;
#else
                    var _defaultSetting = eTargetFrameRateBehaviour.FPS90;
#endif
                    m_TargetFrameRate = new GxSetting<eTargetFrameRateBehaviour>(
                        s_Prefix + nameof(TargetFrameRate),
                        _defaultSetting,
                        _OnSettingUpdate
                    );
                }
                return m_TargetFrameRate;
                void _OnSettingUpdate(eTargetFrameRateBehaviour data)
                {
                    var val = (int)data;
                    Application.targetFrameRate = val;

                    TriggerUpdate();
                }
            }
        }
		#endregion Target Frame Rate

		#region VSync
		public enum VSyncBehaviour
        {
            Off = 0,
            Medium = 1,
            High = 2,
        }

        private GxSetting<VSyncBehaviour> m_VSync;
        public GxSetting<VSyncBehaviour> Vsync
        {
            get
            {
                if (m_VSync == null)
                {
#if UNITY_IPHONE || UNITY_ANDROID
                    var _defaultSetting = VSyncBehaviour.Off;
#else
                    var _defaultSetting = VSyncBehaviour.High;
#endif
                    m_VSync = new GxSetting<VSyncBehaviour>(
                        s_Prefix + nameof(Vsync),
                        _defaultSetting,
                        _OnSettingUpdate
                    );
                }
                return m_VSync;
                void _OnSettingUpdate(VSyncBehaviour data)
                {
                    var val = (int)data;
                    QualitySettings.vSyncCount = val;
                    TriggerUpdate();
                }
            }
        }
		#endregion VSync

		#region VFX Quality
		public enum eQuality
        {
            Low = 0,
            Medium = 1,
            High = 2,
        }

        private GxSetting<eQuality> m_VfxQuality;
        public GxSetting<eQuality> VfxQuality
        {
            get
            {
                if (m_VfxQuality == null)
                {
                    m_VfxQuality = new GxSetting<eQuality>(
                        s_Prefix + nameof(VfxQuality),
                        _GetDefaultQuality(),
                        _OnSettingUpdate
                    );
					_OnSettingUpdate(m_VfxQuality.value);
				}
                return m_VfxQuality;

                eQuality _GetDefaultQuality()
                {
                    int cpuCore = SystemInfo.processorCount;
                    int cpuFreq = SystemInfo.processorFrequency; // MHz
                    int gpuRam = SystemInfo.graphicsMemorySize;
                    if (gpuRam >= 1024 * 8)
                    {
                        return eQuality.High;
                    }
                    else if (gpuRam >= 1024 * 6)
                    {
                        return eQuality.Medium;
                    }

                    return eQuality.Low;
                }
                void _OnSettingUpdate(eQuality setting)
                {
                    switch (setting)
                    {
                        case eQuality.High: QualitySettings.SetQualityLevel(2); break;
                        case eQuality.Medium: QualitySettings.SetQualityLevel(1); break;
                        case eQuality.Low: QualitySettings.SetQualityLevel(0); break;
                        default: QualitySettings.SetQualityLevel(0); break;
                    }
					TriggerUpdate();
				}
            }
        }
		#endregion VFX Quality

		#region PostProcess
		private GxSetting<eQuality> m_PPQuality;
        public GxSetting<eQuality> PostProcessQuality
        {
            get
            {
                if (m_PPQuality == null)
                {
                    m_PPQuality = new GxSetting<eQuality>(
                        s_Prefix + nameof(PostProcessQuality),
                        _GetDefaultQuality(),
                        _OnSettingUpdate
                    );
                    _OnSettingUpdate(m_PPQuality.value);
				}
                return m_PPQuality;

                eQuality _GetDefaultQuality()
                {
                    int cpuCore = SystemInfo.processorCount;
                    int cpuFreq = SystemInfo.processorFrequency; // MHz
                    int gpuRam = SystemInfo.graphicsMemorySize;
                    if (gpuRam >= 1024 * 8)
                    {
                        return eQuality.High;
                    }
                    else if (gpuRam >= 1024 * 6)
                    {
                        return eQuality.Medium;
                    }

                    return eQuality.Low;
                }
                void _OnSettingUpdate(eQuality value)
                {
					/// handle by <see cref="PostProcessingSwitcher"/>
					TriggerUpdate();
				}
            }
        }
		#endregion PostProcess

		#region Screen Resolution
		[System.Serializable]
        public struct ScreenResolutionData : IComparable
        {
            public ScreenResolutionData(int width, int height)
            {
                this.width = width;
                this.height = height;
            }

            [JsonProperty("w")] public int width;
			[JsonProperty("h")] public int height;
            [JsonIgnore] public string label => $"{width}x{height}";
            [JsonIgnore] public Vector2Int resolution => new Vector2Int(width, height);

            public int CompareTo(object obj)
            {
                if (obj is not ScreenResolutionData data)
                    return -1;
                if (data.width == this.width &&
                    data.height == this.height)
                    return 0;
                return -1;
            }


            public static ScreenResolutionData Invalid = new ScreenResolutionData();
			public static ScreenResolutionData Default = new ScreenResolutionData(1920, 1080);
			public static ScreenResolutionData[] Resolutions = new ScreenResolutionData[]
            {
                new ScreenResolutionData(320,240),
                new ScreenResolutionData(1024,768),
                new ScreenResolutionData(1920,1080),
            };

        }

        private GxSetting<ScreenResolutionData> m_ScreenResolution;
        public GxSetting<ScreenResolutionData> ScreenResolution
        {
            get
            {
                if (m_ScreenResolution == null)
                {
                    m_ScreenResolution = new GxSetting<ScreenResolutionData>(
                        s_Prefix + nameof(ScreenResolution),
						GetDefaultScreenResolution(),
                        _OnSettingUpdate
                    );
                    _OnSettingUpdate(m_ScreenResolution.value);
				}
                return m_ScreenResolution;

                void _OnSettingUpdate(ScreenResolutionData setting)
                {
					Debug.Log($"Apply resolution {Color.green.ToRichText($"{setting.width}x{setting.height},{Instance.Fullscreen.ToString()}")}");
                    _OnScreenResolutionUpdate();
				}
            }
        }

        public static ScreenResolutionData GetDefaultScreenResolution()
        {
			var org = Screen.currentResolution;
			var orgRatio = (float)org.width / (float)org.height;

			var preset = ScreenResolutionData.Resolutions;
			(int, float) ratioPt = (-1, float.MaxValue);
			for (int i = 0; i < preset.Length; ++i)
			{
				var pWidth = preset[i].width;
				var pHeight = preset[i].height;

				if (org.width == pWidth &&
					org.height == pHeight)
					return new ScreenResolutionData(org.width, org.height);

				var ratio = (float)pWidth / (float)pHeight;
				if (Mathf.Abs(ratio - orgRatio) < ratioPt.Item2 && // closest ratio.
					pWidth <= org.width && pHeight <= org.height) // ensure width/height within device support range.
				{
					ratioPt = (i, ratio);
				}
			}

			if (ratioPt.Item1 != -1)
			{
                // found.
                var idx = ratioPt.Item1;
                return preset[idx];
			}

			// Worst case, none of them match.
			return ScreenResolutionData.Default;
		}


		private static int m_ScreenResolutionFrameLock = -1;
		private static void _OnScreenResolutionUpdate()
        {
			if (Time.frameCount == m_ScreenResolutionFrameLock)
				return;
			m_ScreenResolutionFrameLock = Time.frameCount;
#if UNITY_STANDALONE
			var scr = Instance.ScreenResolution.value;
			var fullscreen = (FullScreenMode)(int)Instance.Fullscreen.value;
			var rate = Instance.TargetFrameRate.value switch
            {
                eTargetFrameRateBehaviour.FPS30 => 30,
				eTargetFrameRateBehaviour.FPS60 => 60,
				eTargetFrameRateBehaviour.FPS90 => 90,
                _ => 30
			};
            var refreshRate = new RefreshRate
            {
                numerator = (uint)rate,
                denominator = 1u
            };


			var setW = scr.width;
			var setH = scr.height;
			if (setW <= 0 || setH <= 0)
			{
				// special handle.
				setW = 1024;
				setH = 768;
				Debug.LogError($"Invalid ScreenResolution detected, {scr.resolution}, replace by {setW}x{setH}");
			}
			var wantedFull = (fullscreen == FullScreenMode.FullScreenWindow || fullscreen == FullScreenMode.ExclusiveFullScreen);
			var sameMode = (Screen.fullScreen && wantedFull) || (!Screen.fullScreen && !wantedFull);
			var sameSize = (Screen.width == setW && Screen.height == setH);
			var sameRate = (int)Screen.currentResolution.refreshRateRatio.denominator == rate;

			if (sameMode && sameSize && sameRate)
			{
				// skipped, no changed.
				return;
			}

			var orgRatio = (float)setW / (float)setH;
			var arr = Screen.resolutions;
			KeyValuePair<bool, int> match = default;
			KeyValuePair<float, int> closest = new KeyValuePair<float, int>(float.MaxValue, -1);
			for (var i = 0; i < arr.Length && !match.Key; ++i)
			{
				var _w = arr[i].width;
				var _h = arr[i].height;
				var _ratio = _w / (float)_h;
				if (Mathf.Abs(_ratio - orgRatio) < closest.Value && // closest ratio.
					_w <= setW && _h <= setH) // within
				{
					closest = new KeyValuePair<float, int>(_ratio, i);
				}

				if (_w != setW || _h != setH)
					continue;
				match = new KeyValuePair<bool, int>(true, i);
			}

			if (!match.Key) // not found.
			{
				if (closest.Value != -1)
				{
                    // Choose closest ratio
					var k = closest.Value;
					Debug.LogError($"Monitor not supported resolution : {setW}x{setH}, replace by {arr[k].width}x{arr[k].height}.");
					setW = arr[k].width;
					setH = arr[k].height;
				}
				else
				{
                    // Choose the first supported version.
					Debug.LogError($"Monitor not supported resolution : {setW}x{setH}, replace by {arr[0].width}x{arr[0].height}.");
					setW = arr[0].width;
					setH = arr[0].height;
				}
			}

			Screen.SetResolution(setW, setH, fullscreen, refreshRate);
#endif
		}

		public enum eFullScreenBehaviour
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // ExclusiveFullScreen = FullScreenMode.ExclusiveFullScreen,
			FullScreenWindow = FullScreenMode.FullScreenWindow,
			MaximizedWindow = FullScreenMode.MaximizedWindow,
			Windowed = FullScreenMode.Windowed,
#else
            //ExclusiveFullScreen = FullScreenMode.ExclusiveFullScreen,
            FullScreenWindow = FullScreenMode.FullScreenWindow,
            //MaximizedWindow = FullScreenMode.MaximizedWindow,
            Windowed = FullScreenMode.Windowed,
#endif
        }

        private GxSetting<eFullScreenBehaviour> m_FullScreen;
        public GxSetting<eFullScreenBehaviour> Fullscreen
        {
            get
            {
                if (m_FullScreen == null)
                {
                    m_FullScreen = new GxSetting<eFullScreenBehaviour>(
                        s_Prefix + nameof(Fullscreen),
						_GetDefaultValue(),
						_OnSettingUpdated
					);
                    _OnSettingUpdated(m_FullScreen.value);
				}
                return m_FullScreen;
                eFullScreenBehaviour _GetDefaultValue()
                {
#if UNITY_STANDALONE
                    return eFullScreenBehaviour.FullScreenWindow;
#elif UNITY_ANDROID || UNITY_IPHONE
					return eFullScreenBehaviour.FullScreenWindow;
#else
                    return eFullScreenBehaviour.Windowed;
#endif
				}

                void _OnSettingUpdated(eFullScreenBehaviour setting)
                {
                    /// handle by <see cref="ScreenResolution.OnSettingUpdate(ScreenResolutionData)"/>
                    _OnScreenResolutionUpdate();
				}
			}

        }
		#endregion Screen Resolution
	}
}