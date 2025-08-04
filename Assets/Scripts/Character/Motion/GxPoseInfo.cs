using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    [CreateAssetMenu(fileName = "GxPoseInfo", menuName = "Gaia/GxPoseInfo", order = 1)]
    public class GxPoseInfo : ScriptableObject
    {
        public List<string> tags = new List<string>();

        /// <summary>Internal usage for editor generation</summary>
        private KeyValuePair<bool, string> m_Path;
        public void SetPath(string path)
        {
            var dir = KxPath.GetDirectoryName(path);
            this.m_Path = new KeyValuePair<bool, string>(true, dir);
        }
        
        public string start;
        public string loop;
        public string end;
		private bool TryGetPath(string fileName, out string path)
		{
			if (!m_Path.Key)
			{
				path = default;
				return false;
			}

			path = KxPath.Combine(m_Path.Value, $"{fileName}.fbx");
			return true;
		}

		public bool TryGetStartPath(out string path) => TryGetPath(start, out path);
        public bool TryGetLoopPath(out string path) => TryGetPath(loop, out path);
        public bool TryGetEndPath(out string path) => TryGetPath(end, out path);
    }
}