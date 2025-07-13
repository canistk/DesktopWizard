using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class WinPopupCharacterMenu : MonoBehaviour, IWinPopupContent, ISpawnToken
	{
        [SerializeField] private UIText m_Title;
        [SerializeField] private UIButton m_LoadVRMA;

		private GxWinCharacter m_LinkedWin;
		public GxWinCharacter LinkedWin => m_LinkedWin;
		public GxCharacter Character => m_LinkedWin?.Character;

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
			if (Character == null)
			{
				Debug.LogError("Character is not initialized.");
				return;
			}
			if (m_Explorer)
			{
				m_Explorer.SelfDespawn();
				m_Explorer = null;
			}

			Character.CrossFadeVRMA(path);
		}

		public bool Initialized 
		{
			get
			{
				return m_LinkedWin != null && m_Parent != null;
			}
		}
		public void Init(GxWinCharacter winChar)
        {
            this.m_LinkedWin = winChar;
            Debug.Assert(m_LinkedWin != null, "ModelView cannot be null");
			var _name = winChar.gameObject.name;

			this.gameObject.SetActive(true);
			this.gameObject.name = $"Menu: {_name}";

			if (m_Title != null)
            {
                m_Title.Text = $"Menu: {_name}";
			}
		}

		private GxWinPopup m_Parent;
		public void Assign2Popup(GxWinPopup parent)
		{
			this.m_Parent = parent;
			if (m_Parent == null)
				return;

			var form = m_Parent.dwForm;
			if (form != null)
			{
				Debug.Log("linking character menu to form focus event.");
				form.Focus();
				form.Event_LostFocus += DwForm_Event_LostFocus;
				form.FormClosed += DwForm_FormClosed;
			}
		}

		private ISpawner m_Pool;
		public void OnSpawn(ISpawner pool)
		{
			this.m_Pool = pool;
		}
		public void SelfDespawn()
		{
			if (m_Pool == null)
				return;
			if (!Initialized)
				return;
			Debug.Log("despawning character menu [2/2].");
			m_Pool.Despawn(this.gameObject);
			m_Pool = null;
			if (m_LinkedWin != null)
			{
				var form = m_LinkedWin.dwForm;
				if (form != null)
				{
					form.Event_LostFocus -= DwForm_Event_LostFocus;
					form.FormClosed -= DwForm_FormClosed;
				}
			}
			m_LinkedWin = null;
			if (m_Parent != null)
			{
				m_Parent.SelfDespawn();
				m_Parent = null;
			}
			m_Parent = null;
		}

		public void OnDespawn()
		{
			m_Pool = null;
		}

		private void DwForm_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e) => SelfDespawn();
		private void DwForm_Event_LostFocus(uint hWnd, System.EventArgs evt)
		{
			Debug.Log("Lost focus, despawning character menu. [1/2]");
			SelfDespawn();
		}
	}
}