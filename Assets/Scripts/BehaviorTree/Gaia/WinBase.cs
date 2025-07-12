using DesktopWizard;
using System.Collections.Generic;
namespace Gaia
{
    public abstract class WinBase : ActionBase
    {
        private KeyValuePair<bool, GxWin> m_Win;
        protected GxWin ModelView
        {
            get
            {
                if (!m_Win.Key)
                {
                    var comp = gameObject.GetComponent<GxWin>();
                    m_Win = new KeyValuePair<bool, GxWin>(true, comp);
                }
                return m_Win.Value;
            }
        }
		protected DwCamera dwCamera => ModelView.dwCamera;
		protected DwForm dwForm => ModelView.dwForm;
        protected DwWindow dwWindow => ModelView.dwWindow;

		protected sealed override eState InternalUpdate()
		{
			if (ModelView == null)
                return eState.Failure;
            return OnModelViewUpdate();
		}

        protected abstract eState OnModelViewUpdate();

	}
}