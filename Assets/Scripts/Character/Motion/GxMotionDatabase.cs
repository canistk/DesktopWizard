using Kit2;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
    public class GxMotionDatabase
    {
        #region Constructor
        /// <summary>
        /// All motion clips in the database.
        /// </summary>
        // [JsonProperty("clips")]
        [JsonIgnore]
        private List<GxMotionData> m_VRMA = null;

        public GxMotionDatabase()
        {
            m_VRMA = new List<GxMotionData>();
        }
        #endregion Constructor

        #region Singleton
        private static KeyValuePair<bool, GxMotionDatabase> m_Instance = default;
        public static GxMotionDatabase Instance
        {
            get
            {
                if (!m_Instance.Key)
                {
                    if (s_Loading)
                    {
                        Debug.LogWarning("Motion database is currently loading, please wait.");
                        return null;
                    }
                    // start loading motion database
                    EVENT_OnLoaded += OnLoaded;
                    InternalLoading();
                }
                return m_Instance.Value;
            }
        }
        public static event System.Action EVENT_OnLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoLoad()
        {
            ReferenceEquals(Instance, null);
        }
        #endregion Singleton

        #region Loader
        private static bool s_Loading = false;
        private static readonly string DBPath = GxConst.Path.MotionDatabase;
        private static void InternalLoading()
        {
            if (s_Loading)
                throw new System.Exception("Motion database is already loading.");
            s_Loading = true;
            KxFile.Read(DBPath, _Success, _Fail);
            void _Success(string json)
            {
                s_Loading = false;
                var db = GxUtil.FromJson<GxMotionDatabase>(json);
                if (db == null)
                {
                    _Fail(new System.Exception("Fail to deserialize"));
                    return;
                }
                s_Loading = false;
                m_Instance = new KeyValuePair<bool, GxMotionDatabase>(true, db);
                FetchVRMAFolder();
            }

            void _Fail(System.Exception ex)
            {
                s_Loading = false;
                m_Instance = new KeyValuePair<bool, GxMotionDatabase>(true, new GxMotionDatabase());

                FetchVRMAFolder();
            }
        }
        private static void FetchVRMAFolder()
        {
            if (m_Instance.Value == null)
                throw new System.Exception("Motion database is not initialized yet.");

            // Fetch VRMA folder and add to database.
            var VRMAPath = GxConst.Path.VRM;
            KxDirectory.EnsureExists(VRMAPath);
            var motionFilePaths = KxDirectory.GetFiles(VRMAPath, "*.motion", System.IO.SearchOption.AllDirectories);
            var vrmaFilePaths = KxDirectory.GetFiles(VRMAPath, "*.vrma", System.IO.SearchOption.AllDirectories);
            
            int readCount = 0;
            for (int i = 0; i < motionFilePaths.Length; i++)
            {
                if (!KxFile.Exists(motionFilePaths[i]))
                    throw new System.Exception($"Motion file not found: {motionFilePaths[i]}");
                ++readCount;
                KxFile.Read(motionFilePaths[i], _OnMotionJsonRead, _OnMotionJsonReadFail);
            }

            if (motionFilePaths.Length == 0)
            {
                _OnAllMotionRead();
			}

            void _OnMotionJsonRead(string json)
            {
                --readCount;
                try
                {
                    var motion = GxUtil.FromJson<GxMotionData>(json);
                    if (!KxFile.Exists(motion.Path))
                    {
                        // Maintain ".motion" file 1:1 ".vrma" files.
                        Debug.LogError($"{motion.Path} not exist.");
                        var path = KxPath.ChangeExtension(motion.Path, ".motion");
                        KxFile.Delete(path);
                        return;
                    }
                    Instance.m_VRMA.Add(motion);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Fail to deserialize motion file: {ex.Message}");
                }
                if (readCount == 0) _OnAllMotionRead();
            }

            void _OnMotionJsonReadFail(System.Exception ex)
            {
                Debug.LogError($"Fail to read motion file: {ex.Message}");
                --readCount;
                if (readCount == 0) _OnAllMotionRead();
            }

            //// search for undefine VRMA files and add them to the database.
            //// match with existing m_Clips paths.
            //var heartbeat = Time.realtimeSinceStartup;
            //await Task.Delay(1000);
            //while (readCount > 0)
            //{
            //    // wait for all motion files to be read
            //    await Task.Yield();
            //    if (Time.realtimeSinceStartup - heartbeat > 5f)
            //    {
            //        Debug.LogWarning("Fetching VRMA...");
            //        heartbeat = Time.realtimeSinceStartup;
            //    }
            //}


            void _OnAllMotionRead()
            {
                if (!Application.isPlaying)
                {
                    EVENT_OnLoaded?.TryCatchDispatchEventError(o => o?.Invoke());
                    return;
                }

                MatchingVRMA_MotionRecords(vrmaFilePaths);
            }
        }

        private static async void MatchingVRMA_MotionRecords(string[] vrmaFilePaths)
        {
            try
            {
                foreach (var path in vrmaFilePaths)
                {
                    var vrmaPath = KxPath.Fix(path);
                    if (!KxFile.Exists(vrmaPath))
                        throw new System.Exception($"VRMA file not found: {vrmaPath}");

                    var hadRecord = Instance.m_VRMA.Any(o => o.Path == vrmaPath);
                    if (hadRecord)
                        continue; // skip, vrma loading, since we already had reecord.
                    
					var motion = await GenerateMotionByVRMA(vrmaPath);
					if (motion == null)
					{
						// Debug.LogError($"Fail to load VRMA file: {vrmaPath}");
						return;
					}
					Instance.m_VRMA.Add(motion);
					// Write motion to file
					var motionJson = GxUtil.ToJson(motion);
					var motionFilePath = KxPath.ChangeExtension(vrmaPath, ".motion");
					KxFile.Write(motionFilePath, motionJson, backup: false);
					try
					{
						OnNewVRMAFound(motion);
					}
					catch (System.Exception ex)
					{
						Debug.LogError(ex);
					}
				}
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
            finally
            {
                EVENT_OnLoaded?.TryCatchDispatchEventError(o => o?.Invoke());
            }
        }

        private static async Task<GxMotionData> GenerateMotionByVRMA(string vrmaPath)
        {
            RuntimeGltfInstance inst = null;

            try
            {
                var fileName = KxPath.GetFileNameWithoutExtension(vrmaPath);
                // Load VRMA file
                // Get clip length and loop state from VRMA data
                using (var gltf = new UniGLTF.AutoGltfFileParser(vrmaPath).Parse())
                using (var loader = new VrmAnimationImporter(new VrmAnimationData(gltf)))
                {
                    inst = await loader.LoadAsync(new ImmediateCaller());
                    if (inst == null)
                        throw new System.NullReferenceException("Fail to load GLTF instance.");
                    inst.gameObject.name = fileName;
                    float duration;
                    bool isLoop;
                    if (inst.AnimationClips.Count == 0)
                    {
                        throw new System.Exception("VRMA didn't have any animation.");
                    }
                    else if (inst.AnimationClips.Count == 1)
                    {
                        Debug.Log($"VRMA file loaded: {vrmaPath}\nclips: {inst.AnimationClips.Count}");
                        var ani = inst.AnimationClips[0];
                        duration = ani.length;
                        isLoop = ani.isLooping;
                    }
                    else // if (inst.AnimationClips.Count > 1)
                    {
                        Debug.LogWarning($"VRMA file contains multiple animation clips, only the first one will be used: {vrmaPath}");
                        duration = inst.AnimationClips.Sum(clip => clip.length);
                        isLoop = inst.AnimationClips.Any(clip => clip.isLooping);
                    }
                    var motion = new GxMotionData
                    {
                        Key = new GxMotionKey(vrmaPath, eAssetType.VRMA),
                        ClipLength = duration,
                        IsLoop = isLoop,
                        Weight = 1.0f, // Default weight
                    };
                    return motion;
                }
                throw new System.Exception("Fail to load VRMA file, please check the file format and content.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                return null;
            }
            finally
            {
                if (inst != null && inst.gameObject != null)
                    GameObject.DestroyImmediate(inst.gameObject, true);
                inst = null;
            }
        }

        #endregion Loader

        #region Internal API
        private static void OnLoaded()
        {
            EVENT_OnLoaded -= OnLoaded;
            Debug.Log("Motion database ready.");
        }
        private static void OnNewVRMAFound(GxMotionData motion)
        {

        }
        #endregion Internal API

        public static bool TryGetMotion(GxMotionKey key, out GxMotionData motion)
        {
            motion = null;
            foreach (var c in GetMotions())
            {
                if (c.Key.Equals(key))
                {
                    motion = c;
                    return true;
                }
            }
            return false;
        }

        public static int MotionCount()
        {
            var vrma = Instance?.m_VRMA?.Count ?? 0;
            var timeline = GxTimelineCollection.Instance?.MotionCount() ?? 0;
            return vrma + timeline;
		}

		public static GxMotionData GetMotionAt(int index)
        {
			var vrma = Instance.m_VRMA?.Count ?? 0;
			var timeline = GxTimelineCollection.Instance?.MotionCount() ?? 0;
            var cnt = vrma + timeline;
			if (index < 0 || index >= cnt)
                throw new System.IndexOutOfRangeException();
            if (index < vrma)
            {
                return Instance.m_VRMA[index];
            }
            else if (index >= vrma)
            {
                var i = index - vrma;
                return GxTimelineCollection.Instance.GetMotionAt(i);
            }
            return null;
        }

        public static IEnumerable<GxMotionData> GetMotions()
        {
            foreach (var c in Instance.m_VRMA)
                yield return c;
            if (GxTimelineCollection.Instance is GxTimelineCollection inst)
            {
                foreach (var t in inst.Timelines)
                {
                    yield return t;
                }
            }
        }

		public static bool TryGetRandomMotionKey(out GxMotionData motion)
		{
            var cnt = MotionCount();
            var idx = Random.Range(0, cnt);
            motion = GetMotionAt(idx);
            return motion != null;
		}

		public static IEnumerable<GxMotionData> GetMotionByTags(string[] includeTag, string[] excludeTag, int minCount)
		{
			var noIncludeTags = includeTag == null || includeTag.Length == 0 || (includeTag.Length == 1 && includeTag[0].Length == 0);
			var noExcludeTags = excludeTag == null || excludeTag.Length == 0 || (excludeTag.Length == 1 && excludeTag[0].Length == 0);
			if (noIncludeTags && noExcludeTags)
				yield break;
			if (minCount < 0)
				minCount = 0;
			foreach (var m in GetMotions())
			{
				if (m.Tags == null || m.Tags.Count == 0)
					continue;
				if (!noExcludeTags && m.ContainTags(excludeTag) > 0)
					continue; // Skip if any exclude tag is present

				if (noIncludeTags || m.ContainTags(includeTag) >= minCount)
					yield return m;
			}
		}

		public static bool TryGetRandomMotionByTags(string[] includeTag, string[] excludeTag, int minCount, out GxMotionKey key)
		{
			key = GxMotionKey.Invalid;
			var noIncludeTags = includeTag == null || includeTag.Length == 0 || (includeTag.Length == 1 && includeTag[0].Length == 0);
			var noExcludeTags = excludeTag == null || excludeTag.Length == 0 || (excludeTag.Length == 1 && excludeTag[0].Length == 0);

            var arr = GetMotionByTags(includeTag, excludeTag, minCount).ToArray();
            var idx = Random.Range(0, arr.Length);
			key = arr[idx].Key;
            return arr[idx] != null;
		}

        public static bool TryGetPose(string key, out GxPoseData pose)
        {
            pose = null;
            if (GxTimelineCollection.Instance is GxTimelineCollection inst)
            {
                return inst.TryGetPose(key, out pose);
            }
            return false;
        }

        public static IEnumerable<GxPoseData> GetPoses()
        {
            // TODO: VRMA Pose data structure.
            if (GxTimelineCollection.Instance is GxTimelineCollection inst)
            {
                foreach (var p in inst.Poses)
                {
                    yield return p;
                }
            }
        }

		/// <summary>Return poses that contain the specified tags.</summary>
		/// <param name="includeTag">tags should included from the result</param>
		/// <param name="excludeTag">tags to exclude from the result.</param>
		/// <param name="minCount">how many tag pass the test ? (for IncludeTag only)</param>
		/// <returns></returns>
		public static IEnumerable<GxPoseData> GetPosesByTag(string[] includeTag, string[] excludeTag, int minCount)
		{
			var noIncludeTags = includeTag == null || includeTag.Length == 0 || (includeTag.Length == 1 && includeTag[0].Length == 0);
			var noExcludeTags = excludeTag == null || excludeTag.Length == 0 || (excludeTag.Length == 1 && excludeTag[0].Length == 0);
			if (noIncludeTags && noExcludeTags)
				yield break; // No tags provided, nothing to return

			foreach (var p in GetPoses())
            {
				if (p.Tags == null || p.Tags.Count == 0)
					continue;
				if (!noExcludeTags && p.ContainTags(excludeTag) > 0)
					continue; // Skip if any exclude tag is present

				if (noIncludeTags || p.ContainTags(includeTag) >= minCount)
					yield return p;
			}
        }

        public static bool TryGetRandomPoseByTags(string[] includeTags, string[] excludeTags, int minCount, out GxPoseData pose)
		{
            pose = default;
			var filtered = GetPosesByTag(includeTags, excludeTags, minCount).ToArray();
            if (filtered.Length == 0)
                return false;
            var rnd = Random.Range(0, filtered.Length);
            pose = filtered[rnd];
            return pose != null;
        }

		public static bool TryGetRandomPoseKey(out GxPoseData pose)
		{
            // TODO: avoid to array.
            // GxTimelineCollection.Instance.PoseCount();
            var arr = GetPoses().ToArray();
            if (arr.Length == 0)
            {
                pose = default;
                return false;
            }
            var idx = Random.Range(0, arr.Length);
            pose = arr[idx];
            return pose != null;
		}
	}
}