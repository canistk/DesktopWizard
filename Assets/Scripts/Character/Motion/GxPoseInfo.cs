using Kit2;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Gaia
{

    [CreateAssetMenu(fileName = "GxPoseInfo", menuName = "Gaia/GxPoseInfo", order = 1)]
    public class GxPoseInfo : GxMotionData_BuildinInfo
	{
        public List<string> tags = new List<string>();

        /// <summary>Internal usage for editor generation</summary>
        private KeyValuePair<bool, string> m_Path;
        public void SetPath(string path)
        {
            var dir = KxPath.GetDirectoryName(path);
            this.m_Path = new KeyValuePair<bool, string>(true, dir);
        }

        public string key;
        public GxTimelineData start;
        public GxTimelineData loop;
        public GxTimelineData end;

        public void Assign(string key, GxTimelineData start, GxTimelineData loop, GxTimelineData exit, string[] tags)
        {
            this.key = key;
            this.start = start;
            this.loop = loop;
            this.end = exit;
            this.tags = new List<string>(tags);
        }

        public GxPoseData ToData(string dir)
        {
            var s = start.Path;
            var l = loop.Path;
            var e = end.Path;

			var data = new GxPoseData(key,
                new GxMotionKey(s, eAssetType.Timeline),
                new GxMotionKey(l, eAssetType.Timeline),
                new GxMotionKey(e, eAssetType.Timeline));
            return data;
        }
    }
}