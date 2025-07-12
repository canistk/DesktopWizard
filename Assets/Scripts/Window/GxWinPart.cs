using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxWinPart : MonoBehaviour
    {
        private KeyValuePair<bool, GxWin> m_Win;
        public GxWin win
        {
            get
            {
                if (!m_Win.Key)
                    m_Win = new KeyValuePair<bool, GxWin>(true, GetComponentInParent<GxWin>(includeInactive: true));
                return m_Win.Value;
            }
        }
        protected DwCamera dwCamera => win.dwCamera;
        protected DwForm dwForm => win.dwForm;

        protected virtual void Awake() { }

        protected virtual void OnDestroy(){ }

		protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }
	}
}