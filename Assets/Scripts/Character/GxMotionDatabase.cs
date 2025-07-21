using Kit2;
using Kit2.Tasks;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
namespace Gaia
{
    public class GxMotionDatabase
    {
		#region Constructor
		/// <summary>
		/// All motion clips in the database.
		/// </summary>
		[JsonProperty("clips")]
		private List<GxMotionData> m_Clips = null;

		public GxMotionDatabase()
        {
            m_Clips = new List<GxMotionData>();
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
					InternalLoading();
                }
                return m_Instance.Value;
            }
		}
        public static event System.Action EVENT_OnLoaded;
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
				EVENT_OnLoaded?.TryCatchDispatchEventError(o => o?.Invoke());
				Debug.Log($"Motion database loaded");
			}

            void _Fail(System.Exception ex)
            {
                s_Loading = false;
				m_Instance = new KeyValuePair<bool, GxMotionDatabase>(true, new GxMotionDatabase());
				EVENT_OnLoaded?.TryCatchDispatchEventError(o => o?.Invoke());
				Debug.Log($"Motion database created");
			}
		}
		#endregion Loader

        public async void CleanUpInvalidLink()
        {
            var VRMAPath = GxConst.Path.VRM;
            var i = m_Clips.Count;
            while (i --> 0)
            {
                var clip = m_Clips[i];
                switch (clip.Type)
                {
                    case eAssetType.Unknown:
                    break;

                    case eAssetType.VRMA:
                        if (!KxFile.Exists(clip.Path))
                        {
                            m_Clips.RemoveAt(i);
                        }
                    break;

                    case eAssetType.Timeline:
                    {
                        // Ignore checking, assume always correct.
                        //var handle = Addressables.LoadAssetAsync<Object>(clip.Path);
                        //Addressables.Release(handle);
					}
                    break;

                    default:
                    Debug.LogError($"Non-Handle {clip.ToString()}");
                    break;
                }
            }
            Save();
        }

		public void Save()
        {
			var json = GxUtil.ToJson(this);
            var path = GxConst.Path.MotionDatabase;
			KxFile.WriteWithBackup(DBPath, json);
            Debug.Log($"Motion database saved\n{DBPath}");
        }

		public IEnumerable<GxMotionData> GetMotions()
        {
            foreach (var c in m_Clips)
                yield return c;
            if (GxTimelineCollection.Instance is GxTimelineCollection inst)
            {
                foreach (var t in inst.Timelines)
                {
                    yield return t;
                }
            }
        }

        [JsonIgnore]
        public int Count
        {
            get
            {
                var rst = m_Clips == null ? 0 : m_Clips.Count;
                if (GxTimelineCollection.Instance is GxTimelineCollection inst)
				{
                    rst += inst.Count();
                }
                return rst;
			}
        }
	}
}