using DesktopWizard;
using System.Collections.Generic;
namespace Gaia
{
    public abstract class ModelViewBase : ActionBase
    {
        private KeyValuePair<bool, GxModelView> m_ModelView;
        protected GxModelView modelView
        {
            get
            {
                if (!m_ModelView.Key)
                {
                    var comp = gameObject.GetComponent<GxModelView>();
                    m_ModelView = new KeyValuePair<bool, GxModelView>(true, comp);
                }
                return m_ModelView.Value;
            }
        }
		protected DwCamera dwCamera => modelView.dwCamera;
		protected DwForm dwForm => modelView.dwForm;
        protected DwWindow dwWindow => modelView.dwWindow;

		protected sealed override eState InternalUpdate()
		{
			if (modelView == null)
                return eState.Failure;
            return OnModelViewUpdate();
		}

        protected abstract eState OnModelViewUpdate();

	}
}