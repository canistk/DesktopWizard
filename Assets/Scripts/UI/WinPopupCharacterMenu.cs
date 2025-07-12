using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class WinPopupCharacterMenu : UIPopupBase
    {
        [SerializeField] private Camera m_Camera;
        [SerializeField] private Canvas m_Canvas;

		[Space]
		[SerializeField] GxWin m_Win;
        [SerializeField] private UIText m_Title;
        [SerializeField] private UIButton m_LoadVRMA;

		public GxWin win => m_Win;
		private GxWin m_LinkedWin;
		public GxWin LinkedWin => m_LinkedWin;
        private GxCharacter m_Character;
		public GxCharacter Character => m_Character;

		private void Start()
		{
			if (m_LoadVRMA != null)
            {
				m_LoadVRMA.EVENT_OnClick += M_LoadVRMA_EVENT_OnClick;
            }
		}


		private void OnDestroy()
		{
			if (m_LoadVRMA)
			{
				m_LoadVRMA.EVENT_OnClick -= M_LoadVRMA_EVENT_OnClick;
			}

		}

		private void M_LoadVRMA_EVENT_OnClick()
		{
			(var token, var popup) = UIPopup.Explorer(Application.streamingAssetsPath, new[] { ".vrma" }, OnVRMASelected);
			m_Explorer = popup as UIExplorer;
		}
		UIExplorer m_Explorer;
		private void OnVRMASelected(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("VRMA path cannot be null.", this);
				return;
			}
			if (m_Character == null)
			{
				Debug.LogError("Character is not initialized.");
				return;
			}
			if (m_Explorer)
			{
				m_Explorer.SelfDespawn();
				m_Explorer = null;
			}

			m_Character.CrossFadeVRMA(path);
		}

		private bool m_Initialized = false;
		public void Init(int Left, int Top, GxWin modelView, GxCharacter character)
        {
            this.m_LinkedWin = modelView;
            this.m_Character = character;
            Debug.Assert(m_LinkedWin != null, "ModelView cannot be null");
            Debug.Assert(m_Character != null, "Character cannot be null");
			this.gameObject.SetActive(true);
			this.gameObject.name = $"Menu: {modelView.name}";

			if (m_Title != null)
            {
                m_Title.Text = $"Menu: {modelView.name}";
			}

			var form = this.win.dwForm;
			if (form != null)
			{
				form.Event_LostFocus += DwForm_Event_LostFocus;
				form.FormClosed += DwForm_FormClosed;
				form.Focus();
				form.Left = Left;
				form.Top = Top;
			}
			m_Initialized = true;
		}

		private void DwForm_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e) => SelfDespawn();
		private void DwForm_Event_LostFocus(uint hWnd, System.EventArgs evt)
		{
			Debug.Log("Lost focus, despawning character menu. [1/2]");
			SelfDespawn();
		}

		public override void SelfDespawn()
		{
			// base.SelfDespawn();
			if (!m_Initialized)
				return;
			m_Initialized = false;
			Debug.Log("Lost focus, despawning character menu [2/2].");
			this.win.gameObject.SetActive(false);
			if (this.win?.dwForm != null)
			{
				win.dwForm.Event_LostFocus -= DwForm_Event_LostFocus;
				win.dwForm.FormClosed -= DwForm_FormClosed;
			}
			OnDespawn();
		}

		public override void OnDespawn()
		{
			base.OnDespawn();
			m_LinkedWin = null;
            m_Character = null;
		}
	}
}