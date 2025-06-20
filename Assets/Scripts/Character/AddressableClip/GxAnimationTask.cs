using Kit2.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace Gaia
{
	public class GxAnimationTask : GxCharacterTask, IRetarget
	{
		GxTimelineAsset timelineAsset;
		GxRetargeting fromBiped => timelineAsset?.GetRetargeting();

		private MyTask m_Downloader;
		private BlendWeight m_BlendIn;
		private int m_Index = 0;
		private List<MyTaskBase> m_Tasks = new List<MyTaskBase>();
		public GxAnimationTask(GxCharacter character, string assetPath) : base(character)
		{
			m_Downloader = new GxAddressableTask<GxTimelineAsset>(assetPath, _OnAssetLoaded, _ErrorHandler);
			m_Tasks.Add(m_Downloader);
			m_Tasks.Add(new MyTaskAction(() => 
			{
				if (timelineAsset == null)
				{
					Debug.LogError("Timeline asset is null, cannot bind to character.");
					return;
				}
				// timelineAsset.Director.Play();
				if (timelineAsset.isLoop)
					timelineAsset.Director.extrapolationMode = UnityEngine.Playables.DirectorWrapMode.Loop;
				else
					timelineAsset.Director.extrapolationMode = UnityEngine.Playables.DirectorWrapMode.Hold;

				character.Retargeting.AddTarget(this);
			}));
			m_BlendIn = new BlendWeight(0f, 1f, 0.5f, false);
			m_Tasks.Add(m_BlendIn);
		}

		protected override bool InternalExecute()
		{
			if (m_Tasks.Count == 0)
				return false;
			
			if (m_Index < m_Tasks.Count)
			{
				var task = m_Tasks[m_Index];
				if (!task.Execute())
				{
					++m_Index;
				}
			}
			return m_Index > m_Tasks.Count;
		}

		public override void Reset()
		{
			base.Reset();
			foreach (var task in m_Tasks)
			{
				task.Reset();
			}
		}

		private void _OnAssetLoaded(GxTimelineAsset asset)
		{
			timelineAsset = asset;
			Debug.Log($"Timeline loaded: {asset.name}");
		}

		void _ErrorHandler(System.Exception ex)
		{
			Debug.LogError($"Error loading asset: {ex.Message}");
			
		}

		public float GetWeight01()
		{
			return m_BlendIn.weight;
		}

		public GxRetargeting GetTarget()
		{
			return fromBiped;
		}
	}

	public class BlendWeight : MyTaskBase
	{
		private readonly float start,end,duration;
		private readonly bool realTime;
		public float weight { get; private set; } = 0f;
		public BlendWeight(float startWeight01, float targetWeight01, float duration, bool realTime)
		{
			this.start = Mathf.Clamp01(startWeight01);
			this.end = Mathf.Clamp01(targetWeight01);
			this.duration = Mathf.Max(0f, duration);
			this.realTime = realTime;
			this.weight = start; // Initialize weight to start value
		}

		protected KeyValuePair<bool, float> m_StartTime;

		private float GetTime()
		{
			return realTime ? Time.realtimeSinceStartup : Time.time;
		}

		public override bool Execute()
		{
			if (duration <= 0f)
			{
				weight = end; // Instant transition
				return false; // Task is complete
			}

			var time = GetTime();

			if (!m_StartTime.Key)
			{
   				m_StartTime = new KeyValuePair<bool, float>(true, time);
				weight = start;
			}

			float elapsed = time - m_StartTime.Value;
			if (elapsed >= duration)
			{
				weight = end;
				return false; // Task is complete
			}

			// Interpolate the weight based on elapsed time
			float pt = elapsed / duration;
			weight = Mathf.Lerp(start, end, pt);
			return true;
		}

		public override void Reset()
		{
			base.Reset();
			m_StartTime = default;
			weight = start;
		}
	}

	/// <summary>
	/// To download and load an asset from Addressables.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class GxAddressableTask<T> : MyTaskWithState
	{
 		private readonly string m_AssetPath;
		private readonly AssetReference m_AssetReference;
		private readonly bool m_ByRef;
		private AsyncOperationHandle<T> m_Oper;
		public AsyncOperationHandle<T> Operation => m_Oper;
		private System.Action<T> m_Success;
		private System.Action<System.Exception> m_Fail;
		public GxAddressableTask(string assetPath,
			System.Action<T> success,
			System.Action<System.Exception> fail)
		{
			this.m_AssetPath = assetPath;
			this.m_AssetReference = default;
			this.m_ByRef = false;
			this.m_Success = success;
			this.m_Fail = fail;
		}
		public GxAddressableTask(AssetReference assetReference,
			System.Action<T> success,
			System.Action<System.Exception> fail)
		{
			this.m_AssetPath = string.Empty;
			this.m_AssetReference = assetReference;
			this.m_ByRef = true;
			this.m_Success = success;
			this.m_Fail = fail;
		}

		protected override void OnEnter()
		{
			m_Oper = m_ByRef ?
				m_AssetReference.LoadAssetAsync<T>() :
				Addressables.LoadAssetAsync<T>(m_AssetPath);
		}

		protected override bool ContinueOnNextCycle()
		{
			// Continue if the asset is still loading
			if (m_Oper.Status == AsyncOperationStatus.Failed)
				return false;

			return !m_Oper.IsDone;
		}

		protected override void OnComplete()
		{
			Debug.Log($"Successfully loaded asset: {m_Oper.DebugName}");
			try
			{
				if (m_Oper.Status == AsyncOperationStatus.Failed)
				{
					throw m_Oper.OperationException;
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error during asset loading: {ex.Message}");
				m_Fail?.Invoke(ex);
				return;
			}

			m_Success?.Invoke(m_Oper.Result);
		}

		public override void Reset()
		{
			base.Reset();
			// TODO: if it's downloaded, keep the handle for next time.
			if (m_Oper.IsValid())
			{
				Addressables.Release(m_Oper);
				m_Oper = default;
			}
		}
	}
}