using Kit2.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace Gaia
{
	public class GxBlendAniTask : GxCharacterTask
	{
		private enum eState
		{
			Init,
			WaitForAsset,
			BindCharacter,
			BlendIn,
			BlendOut,
			Completed,
			Error = 100, // Error state, can be used for debugging or logging
		}
		private eState m_State = eState.Init;

		GxTimelineAsset timelineAsset;
		private readonly string m_AssetPath;
		private MyTask m_Downloader;
		public GxBlendAniTask(string path, GxCharacter character) : base(character)
		{
			
		}

		protected override void OnEnter()
		{
			if (m_State != eState.Init)
			{
				throw new Exception("Logic error.");
			}
			m_Downloader = new GxAddressableWrapper<GxTimelineAsset>(m_AssetPath, _OnAssetLoaded, _ErrorHandler);
			m_State = eState.WaitForAsset;
		}

		private void _OnAssetLoaded(GxTimelineAsset asset)
		{
			if (m_State != eState.WaitForAsset)
			{
				Debug.LogError("Unexpected state when loading asset.");
				return;
			}

			if (asset == null)
			{
				Debug.LogError("Failed to load timeline asset.");
				return;
			}

			timelineAsset = asset;
			Debug.Log($"Timeline loaded: {asset.name}");
			++m_State;
		}

		protected override bool ContinueOnNextCycle()
		{
			switch (m_State)
			{
				case eState.Init:
					throw new System.InvalidOperationException("Task is not initialized properly.");
				case eState.WaitForAsset:
				if (m_Downloader == null)
					return false;

				if (!m_Downloader.Execute())
					++m_State;
				return true;

				case eState.BindCharacter:
				if (timelineAsset == null)
				{
					Debug.LogError("Timeline asset is null, cannot bind to character.");
					return false;
				}

				// TODO: handle character retargeting & blending curve.
				timelineAsset.Bind(Character);
				timelineAsset.Director.Play();

				// var retargeting = timelineAsset.GetRetargeting();
				// Character.Retargeting.AddTarget()
				++m_State;
				return true;

				case eState.BlendIn:
				return false;// Blending is done, no further action needed.
			}

			throw new System.NotImplementedException();
		}

		void _ErrorHandler(System.Exception ex)
		{
			Debug.LogError($"Error loading asset: {ex.Message}");
			m_State = eState.Error;
		}

		protected override void OnComplete()
		{
			throw new NotImplementedException();
		}
	}

	public class GxAddressableWrapper<T> : MyTaskWithState
	{
 		private readonly string m_AssetPath;
		private readonly AssetReference m_AssetReference;
		private readonly bool m_ByRef;
		private AsyncOperationHandle<T> m_Oper;
		public AsyncOperationHandle<T> Operation => m_Oper;
		private System.Action<T> m_Success;
		private System.Action<System.Exception> m_Fail;
		public GxAddressableWrapper(string assetPath,
			System.Action<T> success,
			System.Action<System.Exception> fail)
		{
			this.m_AssetPath = assetPath;
			this.m_AssetReference = default;
			this.m_ByRef = false;
			this.m_Success = success;
			this.m_Fail = fail;
		}
		public GxAddressableWrapper(AssetReference assetReference,
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
	}
}