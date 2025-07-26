using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace Gaia
{
	using DB = GxMotionDatabase;
	[RequireComponent(typeof(GxCharacter))]
	public class GxMotionHelper : MonoBehaviour
    {
        private GxCharacter m_Character;
        public GxCharacter character
        {
            get
            {
                if (m_Character == null)
                {
                    m_Character = GetComponent<GxCharacter>();
				}
                return m_Character;
			}
        }

        [Header("Addressable, Animation Clips")]
        public int m_FirstIndex = 0;
        public int m_SecondIndex = 0;
		public int m_PoseIndex = 0;
		public float m_FadeIn = 0.2f;
		GxMotionKey[] m_Data;
		public GxMotionKey[] data
		{
			get
			{
				if (m_Data == null)
				{
					m_Data = DB.GetMotions().Select(o => o.Key).ToArray();
				}
				return m_Data;
			}
		}

		string[] m_PoseKeys;
		public string[] poseKeys
		{
			get
			{
				if (m_PoseKeys == null)
				{
					m_PoseKeys = DB.GetPoses().Select(o => o.key).ToArray();
				}
				return m_PoseKeys;
			}
		}

		public void Editor_AnimationClip(int idx)
		{
#if UNITY_EDITOR
			if (idx < 0 || idx >= data.Length)
			{
				Debug.LogWarning($"Invalid index {idx}. Must be between 0 and {data.Length - 1}.");
				return;
			}
			var o = data[idx];
			character.CrossFade(o, m_FadeIn);
#endif
		}
	}
}