using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class UIPopupCharacterMenu : UIPopupBase
    {
        [SerializeField] private Camera m_Camera;
        [SerializeField] private Canvas m_Canvas;

		[Space]
		[SerializeField] GxModelView m_ModelView;
        [SerializeField] private UIText m_Title;
        [SerializeField] private UIButton m_LoadVRMA;

		public GxModelView modelView => m_ModelView;
		private GxModelView m_LinkedModelView;
		public GxModelView LinkedModelView => m_LinkedModelView;
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
			(var token, var popup) = UIPopup.Explorer(Application.streamingAssetsPath, new[] { ".vrma" },
				(path) => {

			});
			if (popup == null)
			{
				Debug.LogError("Failed to open explorer for VRMA files.");
				return;
			}
			

		}

		private bool m_Initialized = false;
		public void Init(int Left, int Top, GxModelView modelView, GxCharacter character)
        {
            this.m_LinkedModelView = modelView;
            this.m_Character = character;
            Debug.Assert(m_LinkedModelView != null, "ModelView cannot be null");
            Debug.Assert(m_Character != null, "Character cannot be null");
			this.gameObject.SetActive(true);
			this.gameObject.name = $"Menu: {character.name}";

			if (m_Title != null)
            {
                m_Title.Text = $"Menu: {character.name}";
			}

			var form = this.modelView.dwForm;
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
			this.modelView.gameObject.SetActive(false);
			if (this.modelView?.dwForm != null)
			{
				modelView.dwForm.Event_LostFocus -= DwForm_Event_LostFocus;
				modelView.dwForm.FormClosed -= DwForm_FormClosed;
			}
			OnDespawn();
		}

		public override void OnDespawn()
		{
			base.OnDespawn();
			m_LinkedModelView = null;
            m_Character = null;
		}
	}
}