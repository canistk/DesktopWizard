using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    /// <summary>
    /// A helper class for generate <see cref="GxTimelineCollection"/>
    /// usage : assume the same file name Timeline will use this as 
    /// <see cref="GxTimelineCollection.TimelineData"/> source.
    /// </summary>
    [CreateAssetMenu(fileName = "GxTimelineInfo", menuName = "Gaia/GxTimelineInfo", order = 1)]
    public class GxTimelineInfo : ScriptableObject
    {
        public List<string> tags = new List<string>();
    }
}