using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxModelPart : MonoBehaviour
    {
        private KeyValuePair<bool, GxModelView> m_ModelView;
        public GxModelView modelView
        {
            get
            {
                if (!m_ModelView.Key)
                    m_ModelView = new KeyValuePair<bool, GxModelView>(true, GetComponentInParent<GxModelView>(includeInactive: true));
                return m_ModelView.Value;
            }
        }
        protected DwCamera dwCamera => modelView.dwCamera;
        protected DwForm dwForm => modelView.dwForm;

        protected virtual void Awake() { }

        protected virtual void OnDestroy(){ }

		protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }
	}
}