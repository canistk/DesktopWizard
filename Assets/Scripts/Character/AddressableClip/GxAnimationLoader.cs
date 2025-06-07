using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace Gaia
{
    public class GxAnimationLoader : MonoBehaviour
    {
		[SerializeField] Animator m_Animator = null;
		[SerializeField] Animation m_Animation = null;

		[SerializeField] string m_AnimationPath = "Assets/Scripts/Character/SMB/TestAni01.asset";
		[Space]
		[SerializeField] bool m_UseRef = false;
		[SerializeField] AssetReference m_AnimationRef;
		private void OnEnable()
		{
			if (m_UseRef)
			{
   				if (m_AnimationRef == null)
				{
					Debug.LogError("Animation reference is not set.");
					return;
				}
				m_AnimationRef.LoadAssetAsync<GxTimelineAsset>().Completed += (op) => {
					if (op.Status == AsyncOperationStatus.Succeeded)
					{
						Debug.Log($"Loaded {op.Result.name} from reference.");
					}
					else
					{
						Debug.LogError($"Failed to load animation from reference: {op.OperationException.Message}");
					}
				};
			}
			else
			{
				Load<GxTimelineAsset>(m_AnimationPath, _MyOnloaded);
			}
		}

		private void _MyOnloaded(GxTimelineAsset ani)
		{
			Debug.Log($"Load {ani} completed");

			if (m_Animator == null)
			{
				Debug.LogError("Animator is not set.");
				return;
			}

			// Add the animation clip to the animator, with GxSMB as the state machine behaviour
			var ctrl = m_Animator.runtimeAnimatorController as AnimatorOverrideController;
			if (ctrl == null)
			{
				Debug.LogError("Animator controller is not set.");
				return;
			}

			// If the animation clip is not found, add it
			//var newClip = Instantiate(ani.animationClip);
			//newClip.name = ani.animationClip.name;

			//m_Animation.AddClip(newClip, newClip.name);
			//m_Animation.PlayQueued(newClip.name, QueueMode.PlayNow, PlayMode.StopSameLayer);
		}

		private void OnDisable()
		{

		}



		private abstract class LoadInfo : System.IDisposable
		{
			public AsyncOperationHandle handle { get; private set; }
			private bool isDisposed;

			public LoadInfo(AsyncOperationHandle handle)
			{
				this.handle = handle;
				handle.Completed += _OnLoadCompleted;
			}
			protected abstract void _OnLoadCompleted(AsyncOperationHandle op);

			public void Unload()
			{
				Dispose(true);
			}

			#region IDisposable Support
			protected abstract void OnDispose(bool disposing);
			protected void Dispose(bool disposing)
			{
				if (isDisposed)
					return;
				if (disposing)
				{
					if (handle.IsValid())
					{
						Addressables.Release(handle);
					}
				}
				OnDispose(disposing);
				handle = default;
				isDisposed = true;
			}

			~LoadInfo()
			{
				Dispose(disposing: false);
			}

			public void Dispose()
			{
				Dispose(disposing: true);
				System.GC.SuppressFinalize(this);
			}
			#endregion IDisposable Support
		}
		private class LoadInfo<OBJ> : LoadInfo
			where OBJ : UnityEngine.Object
		{
			public List<LoadCallback<OBJ>> callbacks;
			public LoadInfo(AsyncOperationHandle handle, LoadCallback<OBJ> callback) : base(handle)
			{
				this.callbacks = new List<LoadCallback<OBJ>>();
				if (callback != null)
				{
					this.callbacks.Add(callback);
				}
			}

			protected override void _OnLoadCompleted(AsyncOperationHandle op)
			{
				var obj = handle.Result as OBJ;
				foreach (var cb in callbacks)
				{
					try
					{
						cb?.Invoke(obj);
					}
					catch (System.Exception e)
					{
						Debug.LogError($"Error invoking callback: {e.Message}");
					}
				}
			}

			protected override void OnDispose(bool disposing)
			{
				if (disposing)
				{
					this.callbacks.Clear();
				}
				this.callbacks = null;
			}
		}

		private static Dictionary<string, LoadInfo> s_OperDict = new Dictionary<string, LoadInfo>();

		public delegate void LoadCallback<OBJ>(OBJ callback) where OBJ : UnityEngine.Object;
		public void Load<OBJ>(string path, LoadCallback<OBJ> callback)
			where OBJ : UnityEngine.Object
		{
			var handle = Addressables.LoadAssetAsync<OBJ>(path);
			if (s_OperDict.TryGetValue(path, out var info))
			{
				if (info.handle.IsDone)
				{
					// If the handle is already completed, invoke the callback immediately
					var obj = info.handle.Result as OBJ;
					callback?.Invoke(obj);
				}
				else
				{
					var infoWithObj = info as LoadInfo<OBJ>;
					infoWithObj.callbacks.Add(callback);
				}
				return;
			}
			info = new LoadInfo<OBJ>(handle, callback);
			s_OperDict.Add(path, info);
		}
	}
}