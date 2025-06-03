using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class MyDelayActivator : MonoBehaviour
    {
        [System.Serializable]
        private struct DelayObj
        {
            public Transform transform;
            public float delay;
            public bool handleDisable;
        }

        [SerializeField] List<DelayObj> targets = new List<DelayObj>();
        private eState[] m_Flags = new eState[0];
        private enum eState
        {
            NullRef = -1,
            Unknown = 0,
            Wait4Active,
            Activated,
        }
        private float m_OnEnable = 0;

		private void OnEnable()
		{
            m_OnEnable = Time.timeSinceLevelLoad;
			m_Flags = new eState[targets.Count];
			for (int i = 0; i < targets.Count; ++i)
			{
				if (targets[i].transform == null)
                {
                    m_Flags[i] = eState.NullRef;
                    continue;
                }
            }
		}

		private void Update()
		{
            if (m_Flags == null || targets == null)
                return;
            if (m_Flags.Length != targets.Count)
                return;
            var passed = Time.timeSinceLevelLoad - m_OnEnable;
			for (int i = 0; i < targets.Count; ++i)
			{
                switch (m_Flags[i])
                {
                    default:
                    case eState.NullRef:
                    case eState.Activated:
                        continue;
                    case eState.Unknown:
                    case eState.Wait4Active:
                        m_Flags[i] =
							targets[i].transform == null ? eState.NullRef :
                            (passed >= targets[i].delay) ? eState.Activated :
                            eState.Wait4Active;
                        if (m_Flags[i] == eState.Activated)
                        {
                            targets[i].transform.gameObject.SetActive(true);
						}
                        break;
                }
			}
		}

		private void OnDisable()
		{
            m_OnEnable = 0f;
            for (int i = 0; i < targets.Count; ++i)
            {
                if (targets[i].transform != null && targets[i].handleDisable)
                    targets[i].transform.gameObject.SetActive(false);
                m_Flags[i] = eState.Unknown;
            }
		}
	}
}