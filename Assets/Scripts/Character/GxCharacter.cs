using Kit2;
using Kit2.ObjectPool;
using Kit2.Tasks;
using System.Collections;
using System.Collections.Generic;
using UniGLTF;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
    public class GxCharacter : MonoBehaviour
    {
        
		private void Reset()
		{
            Editor_RetargetingCreate();
			Editor_ObjectPoolCreate();
		}

		public void RuntimeCreation()
		{
			Editor_RetargetingCreate();
			if (m_Retargeting != null)
			{
				m_Retargeting.ForceTPose();
			}
			Editor_ObjectPoolCreate();
			#if DEBUG
			_ = transform.GetOrAddComponent<GxTimelineHelper>();
			#endif
		}

		private void Update()
		{
            HandleTasks();
		}

		#region Task Management
		private List<MyTaskBase> m_Tasks = new List<MyTaskBase>();
        private void HandleTasks()
        {
			MyTaskHandler.ManualParallelUpdate(m_Tasks);
		}
		#endregion Task Management

		#region Object Pooling
		[SerializeField] KxObjectPool m_Pool;
        private void Editor_ObjectPoolCreate()
        {
			// m_Pool = transform.GetOrAddComponent<kObjectPool>();
			m_Pool = GetComponentInChildren<KxObjectPool>();
			if (m_Pool == null)
			{
				var tran = new GameObject("Loader").transform;
				tran.SetParent(transform, false);
				tran.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
				m_Pool = tran.gameObject.AddComponent<KxObjectPool>();
			}
		}
		#endregion Object Pooling

		#region Wrapped Retargeting Methods
		[SerializeField] GxRetargeting m_Retargeting;
		private bool m_Fetched = false;
		public Animator animator
		{
			get
			{
				if (m_Retargeting == null && !m_Fetched)
				{
					m_Retargeting = GetComponentInChildren<GxRetargeting>();
					m_Fetched = true;
					if (m_Retargeting == null)
						throw new System.NullReferenceException("GxCharacter requires a Retargeting component in its children.");
					if (m_Retargeting.animator == null)
						throw new System.NullReferenceException("Retargeting requires an Animator component.");
				}
				return m_Retargeting.animator;
			}
		}

		private void Editor_RetargetingCreate()
        {
			m_Retargeting = GetComponentInChildren<GxRetargeting>();
			if (m_Retargeting == null)
			{
				var animator = GetComponentInChildren<Animator>();
				if (animator == null)
				{
					Debug.LogError($"Unable to create {nameof(GxRetargeting)} required component animator.");
				}
				m_Retargeting = animator.gameObject.AddComponent<GxRetargeting>();
			}
		}
        private void InternalPlayTimeline(GxTimelineAsset timelineAsset, float fadeIn, bool realTime)
        {
            if (timelineAsset == null)
                throw new System.ArgumentNullException(nameof(timelineAsset), "Timeline asset cannot be null.");
            var aniTask = new GxAnimationTask(this, timelineAsset, fadeIn, realTime);

			// Hack : while retargeting system is using Update (prefer to use LateUpdate instead),
            // we need to disable animator in Update to prevent animation flickering/overrided.
			if (animator.enabled)
            {
                Debug.LogWarning("Retargeting is enabled in Update, disabling it to prevent flickering. Consider using LateUpdate for retargeting.");
				if (!m_Retargeting.IsLateUpdate)
                    animator.enabled = false;
            }

            m_Tasks.Add(aniTask);
        }

		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;



		public void CrossFade(string timelineAssetPath, float fadeIn, eSrcType type, bool realTime = false)
		{
			if (string.IsNullOrEmpty(timelineAssetPath))
				throw new System.ArgumentNullException(nameof(timelineAssetPath), "Timeline asset path cannot be null or empty.");

			var isVrmaPath = timelineAssetPath.EndsWith(".vrma", IGNORE);
			if (isVrmaPath)
			{
				// Force to GameObject for VRMA assets
				CrossFade_VRMA(timelineAssetPath, fadeIn, eSrcType.GameObject, realTime);
				return; // stop here, wait for VRMA loaded.
			}
			else
			{
				var timelineAssetGo = m_Pool.Spawn(timelineAssetPath, type, null, false);
				_CrossFade_Timeline(timelineAssetGo, timelineAssetPath, fadeIn, realTime);
			}
		}

		private Dictionary<string, GameObject /* prefab */> m_VrmaDict = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);

		private void CrossFade_VRMA(string timelineAssetPath, float fadeIn, eSrcType type, bool realTime = false)
		{
  			if (string.IsNullOrEmpty(timelineAssetPath))
				throw new System.ArgumentNullException(nameof(timelineAssetPath), "Timeline asset path cannot be null or empty.");
			// Force to GameObject for VRMA assets
			type = eSrcType.GameObject;
			if (!m_VrmaDict.TryGetValue(timelineAssetPath, out var _vrmaPrefab))
			{
				// load VRMA asset directly if not in pool
				_InternalLoadVRMA(timelineAssetPath, (gltf) =>
				{
					var prefab = gltf.gameObject;
					if (prefab == null)
						throw new System.Exception($"Failed to load VRMA prefab from path: {timelineAssetPath}");
					SetupVRMAPrefab(prefab);

					m_VrmaDict.Add(timelineAssetPath, _vrmaPrefab = prefab);
					var token = m_Pool.Spawn(prefab, eSrcType.GameObject, m_Pool.transform, false);
					_OnVRMATokenLoaded(token, timelineAssetPath, fadeIn, realTime);

				}, Debug.LogException);
				return; // stop here, wait for VRMA loaded.
			}
			else
			{
				// Use exist prefab to spawn
				var token = m_Pool.Spawn(_vrmaPrefab, eSrcType.GameObject, m_Pool.transform, false);
				_OnVRMATokenLoaded(token, timelineAssetPath, fadeIn, realTime);
			}
			return;

			void SetupVRMAPrefab(GameObject prefab)
			{
				if (prefab == null)
					throw new System.Exception($"Failed to load VRMA prefab from path: {timelineAssetPath}");
				prefab.SetActive(false); // ensure prefab is inactive before spawning

				var vrma = prefab.GetComponent<Vrm10AnimationInstance>();
				if (vrma)
				{
					vrma.ShowBoxMan(false); // hide debug mesh if exists
				}

				var animator = prefab.GetComponentInChildren<Animator>();
				var on = animator.enabled;
				animator.enabled = false;
				Debug.Assert(animator != null, "Animator component not found in the VRMA instance.");
				var retargeting = animator.gameObject.AddComponent<GxRetargeting>();
				retargeting.ForceTPose();
				animator.enabled = on; // restore animator state

				var animation = prefab.GetComponent<Animation>();
				animation.cullingType = AnimationCullingType.AlwaysAnimate;
			}

			async void _InternalLoadVRMA(string path,
				System.Action<RuntimeGltfInstance> loaded,
				System.Action<System.Exception> fail = null)
			{
				try
				{
					if (string.IsNullOrEmpty(path))
						throw new System.ArgumentNullException(nameof(path), "VRMA path cannot be null or empty.");
					using (GltfData data = new AutoGltfFileParser(path).Parse())
					using (var loader = new VrmAnimationImporter(new VrmAnimationData(data)))
					{
						var instance = await loader.LoadAsync(new ImmediateCaller());
						loaded?.Invoke(instance);
					}
				}
				catch (System.Exception ex)
				{
					if (fail == null)
					{
						Debug.LogException(ex);
						return;
					}
					fail.Invoke(ex);
				}
			}

			void _OnVRMATokenLoaded(GameObject token, string timelineAssetPath, float fadeIn, bool realTime)
			{
				var gltf = token.GetComponent<RuntimeGltfInstance>();
				if (gltf == null)
					throw new System.Exception($"{nameof(RuntimeGltfInstance)} not found.");
				var aniTask = new GxVRMA(this, gltf, 0.25f, false, m_Pool, token);
				m_Tasks.Add(aniTask);
			}
		}

		private void _CrossFade_Timeline(GameObject timelineAssetGo, string timelineAssetPath, float fadeIn, bool realTime)
		{
            if (timelineAssetGo == null)
            {
                Debug.LogError($"Failed to spawn timeline asset from path: {timelineAssetPath}");
                return;
			}
            var timelineAsset = timelineAssetGo.GetComponent<GxTimelineAsset>();
            if (timelineAsset == null)
            {
                Debug.LogError($"The spawned GameObject does not have a GxTimelineAsset component: {timelineAssetGo.name}");
                return;
            }
            InternalPlayTimeline(timelineAsset, fadeIn, realTime);
		}

        /// <summary>Called by <see cref="GxAnimationTask"/></summary>
        /// <param name="ani"></param>
        internal void BoardcastWillPlayAnimation(IRetarget ani)
        {
            foreach (var at in GetActiveAnimations())
            {
                if (at == ani)
                    continue;
                at.OnWillPlayAnimation(ani);
			}
        }

		/// <summary>Called by <see cref="GxAnimationTask"/></summary>
		internal void BoardCastPlayedOnce(IRetarget ani)
        {
            
		}

        public IEnumerable<IRetarget> GetActiveAnimations()
        {
            foreach (var task in m_Tasks)
            {
                if (task is not IRetarget aniTask)
                    continue;
                if (task is MyTask t && t.isCompleted)
                    continue;
                yield return aniTask;
			}
		}

        public void AddAnimationRetarget(IRetarget target)
        {
            if (m_Retargeting == null)
            {
                Debug.LogError("GxRetargeting is not initialized.");
                return;
            }
            m_Retargeting.AddTarget(target);
		}

        public void RemoveAnimationRetarget(IRetarget target)
        {
            if (m_Retargeting == null)
            {
                Debug.LogError("GxRetargeting is not initialized.");
                return;
            }
            m_Retargeting.RemoveTarget(target);
		}
		#endregion Wrapped Retargeting Methods

		#region Face Rig
		private KeyValuePair<bool, FaceRig> m_FaceRig;
		public FaceRig FaceRig
		{
			get
			{
				if (!m_FaceRig.Key)
				{
					m_FaceRig = new KeyValuePair<bool, FaceRig>(true, GetComponentInChildren<FaceRig>(true));
					Debug.Assert(m_FaceRig.Value != null, "FaceRig component is missing in the children of GxCharacter.");
				}
				return m_FaceRig.Value;
			}
		}

		#endregion Face Rig

		#region Emotion Wheel
		private KeyValuePair<bool, EmotionWheel> m_EmotionWheel;
		public EmotionWheel EmotionWheel
		{
			get
			{
				if (!m_EmotionWheel.Key)
				{
					m_EmotionWheel = new KeyValuePair<bool, EmotionWheel>(true, GetComponentInChildren<EmotionWheel>(true));
					Debug.Assert(m_EmotionWheel.Value != null, "EmotionWheel component is missing in the children of GxCharacter.");
				}
				return m_EmotionWheel.Value;
			}
		}
		#endregion Emotion Wheel
	}
} 