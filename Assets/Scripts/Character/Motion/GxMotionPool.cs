using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniGLTF;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;
using UniVRM10;

namespace Gaia
{
    public class GxMotionPool : MonoBehaviour
    {
        private static GxMotionPool s_Instance;
        public static GxMotionPool Instance
        {
            get
            {
                if (s_AppQuit)
                    return null;
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<GxMotionPool>();
                    if (s_Instance == null)
                    {
                        // s_Instance = 
                        new GameObject(nameof(GxMotionPool)).AddComponent<GxMotionPool>();
                    }
                }
                return s_Instance;
            }
        }
        private AsyncObjectPool m_Pool;

        private void Awake()
        {
            if (s_AppQuit)
                return;
            if (s_Instance != null)
            {
                gameObject.SetActive(false);
                GameObject.Destroy(gameObject);
                return;
            }
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
            m_Pool = gameObject.GetComponent<AsyncObjectPool>();
            if (m_Pool == null)
                m_Pool = gameObject.AddComponent<AsyncObjectPool>();
        }


        private static bool s_AppQuit = false;
        private void OnApplicationQuit()
        {
            s_AppQuit = true;
        }

		// TODO: preload instead of loading on demand
        //public void Preload(GxMotionKey key,
        //    System.Action<GxMotionHandler> success,
        //    System.Action<System.Exception> fail)
        //{
        //}

        public async Task<GxMotionHandler> GetHandler(GxMotionKey key, Transform parent, float fadeIn)
        {
            if (key.Valid == false)
                throw new System.Exception($"Invalid motion key: {key}");
            if (!GxMotionDatabase.TryGetMotion(key, out GxMotionData motionData))
                throw new System.Exception($"Motion data not found for key: {key}");
            if (motionData.Type == eAssetType.VRMA)
            {
                return await SpawnVRMA(motionData, parent, fadeIn);
            }
            else if (motionData.Type == eAssetType.Timeline)
            {
                return await SpawnTimeline(motionData, parent, fadeIn);
			}

            throw new System.Exception($"Unknown motion type: {motionData.Type} for key: {key}");
        }


        private Dictionary<string, GxVRMAToken /* prefab */> m_VrmaPrefabDict = new Dictionary<string, GxVRMAToken>(System.StringComparer.OrdinalIgnoreCase);
        private async Task<GxVRMAHandler> SpawnVRMA(GxMotionData motionData, Transform parent, float fadeIn)
        {
            if (motionData.Type != eAssetType.VRMA)
                throw new System.Exception($"Motion data is not VRMA type: {motionData}");
            if (string.IsNullOrEmpty(motionData.Path))
                throw new System.Exception($"Motion data path is empty: {motionData}");
            var path = motionData.Path;
            GameObject token = null;
            if (!m_VrmaPrefabDict.TryGetValue(motionData.Path, out var _vrmaPrefab))
            {
                // VRMA prefab not yet loaded, Try load it.
                byte[] bytes = null;
                using (FileStream fs = File.OpenRead(path))
                using (BinaryReader binaryReader = new BinaryReader(fs))
                {
                    bytes = binaryReader.ReadBytes((int)fs.Length);
                }
                using GltfData data = new GlbLowLevelParser(string.Empty, bytes).Parse();
                using (var loader = new VrmAnimationImporter(new VrmAnimationData(data)))
                {
                    IAwaitCaller awaitCaller = Application.isPlaying
                        ? new RuntimeOnlyAwaitCaller(0.5f)
                        : new ImmediateCaller();

                    var gltf = await loader.LoadAsync(awaitCaller);
                    var prefab = gltf.gameObject;
                    prefab.transform.SetParent(transform, false);
                    prefab.name = motionData.ToShortString();
                    prefab.SetActive(false);
                    prefab.hideFlags = HideFlags.HideAndDontSave;
                    // prefab.hideFlags = HideFlags.DontSave;
                    var vrmaToken = prefab.AddComponent<GxVRMAToken>();
                    m_VrmaPrefabDict.Add(motionData.Path, vrmaToken);

                    token = await m_Pool.Spawn(prefab, parent);
                }
            }
            else
            {
                // VRMA prefab already loaded, use it directly.
                token = await m_Pool.Spawn(_vrmaPrefab.gameObject, null, false);
            }
            if (token == null)
                throw new System.Exception($"Failed to spawn VRMA token for motion data: {motionData}");

            token.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			var comp = token.GetComponent<GxVRMAToken>();
            var handler = new GxVRMAHandler(motionData.Key, comp, fadeIn);
            return handler;
        }

        private async Task<GxTimelineHandler> SpawnTimeline(GxMotionData motionData, Transform parent, float fadeIn)
        {
            if (motionData.Type != eAssetType.Timeline)
                throw new System.Exception($"Motion data is not Timeline type: {motionData}");
            if (string.IsNullOrEmpty(motionData.Path))
                throw new System.Exception($"Motion data path is empty: {motionData}");
            var token = await m_Pool.Spawn(motionData.Path, eSrcType.Addressable, parent, false);

			token.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			var timelineAsset = token.GetComponent<GxTimelineAsset>();
			if (timelineAsset == null)
				throw new System.Exception($"The spawned GameObject does not have a GxTimelineAsset component: {token.name}");
			var handler = new GxTimelineHandler(motionData.Key, timelineAsset, fadeIn);
            return handler;
		}
    }
}