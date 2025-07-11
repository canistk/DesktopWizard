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

		private void Awake()
		{
			if (m_LoadVRMA != null)
            {
				m_LoadVRMA.EVENT_OnClick += M_LoadVRMA_EVENT_OnClick;
            }
			modelView.dwForm.Event_LostFocus += DwForm_Event_LostFocus;
			modelView.dwForm.FormClosed += DwForm_FormClosed;
		}


		private void OnDestroy()
		{
			if (m_LoadVRMA)
			{
				m_LoadVRMA.EVENT_OnClick -= M_LoadVRMA_EVENT_OnClick;
			}

			modelView.dwForm.Event_LostFocus -= DwForm_Event_LostFocus;
			modelView.dwForm.FormClosed -= DwForm_FormClosed;
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

		public void Init(GxModelView modelView, GxCharacter character)
        {
            this.m_LinkedModelView = modelView;
            this.m_Character = character;
            Debug.Assert(m_LinkedModelView != null, "ModelView cannot be null");
            Debug.Assert(m_Character != null, "Character cannot be null");

			if (m_Title != null)
            {
                m_Title.Text = $"Menu: {character.name}";
			}

			modelView.dwForm.Focus();
			gameObject.SetActive(true);
		}

		private void DwForm_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e) => SelfDespawn();
		private void DwForm_Event_LostFocus(uint hWnd, System.EventArgs evt) => SelfDespawn();

		public override void SelfDespawn()
		{
			// base.SelfDespawn();
			gameObject.SetActive(false);
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