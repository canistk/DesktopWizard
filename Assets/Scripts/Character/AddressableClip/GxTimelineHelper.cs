using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
namespace Gaia

{
    public class GxTimelineHelper : MonoBehaviour
    {
        [SerializeField] private GxTimelineCollection m_Database;
        public GxTimelineCollection db => m_Database;

        [SerializeField] private GxCharacter m_Character;
        public GxCharacter character => m_Character;

        public int m_FirstIndex = 0;
        public int m_SecondIndex = 0;

        public float m_FadeIn = 0.2f;

		private void Reset()
		{
			m_Character = GetComponent<GxCharacter>();
            m_Database = Resources.Load<GxTimelineCollection>("GxTimelineCollection");
		}
	}
}