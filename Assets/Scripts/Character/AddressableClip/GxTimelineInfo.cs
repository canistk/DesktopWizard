using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Gaia
{
    /// <summary>
    /// A helper class for generate <see cref="GxTimelineCollection"/>
    /// usage : assume the same file name Timeline will use this as 
    /// <see cref="GxTimelineCollection.GxTimelineData"/> source.
    /// </summary>
    [CreateAssetMenu(fileName = "GxTimelineInfo", menuName = "Gaia/GxTimelineInfo", order = 1)]
    public class GxTimelineInfo : GxMotionData_BuildinInfo
	{
        public List<string> tags = new List<string>();
		public bool isLoop;
		public float duration;

        public void Assign(bool isLoop, float duration, string[] tags)
        {
            this.isLoop = isLoop;
            this.duration = duration;
            this.tags = new List<string>(tags);
        }

        public GxTimelineData ToData(string Path)
        {
            var data = new GxTimelineData(Path, isLoop, duration);
            return data;
        }
	}
}