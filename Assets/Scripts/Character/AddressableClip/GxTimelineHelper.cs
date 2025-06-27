using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
namespace Gaia

{
    [RequireComponent(typeof(GxCharacter))]
	public class GxTimelineHelper : MonoBehaviour
    {
        [SerializeField] private GxTimelineCollection m_Database;
        public GxTimelineCollection db => m_Database;

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

        public int m_FirstIndex = 0;
        public int m_SecondIndex = 0;

        public float m_FadeIn = 0.2f;

		private void Reset()
		{
            m_Database = Resources.Load<GxTimelineCollection>("GxTimelineCollection");
		}

		private void Awake()
		{
			if (m_Database == null)
            {
				m_Database = Resources.Load<GxTimelineCollection>("GxTimelineCollection");
			}
		}
	}
}