using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class WinPopupCharacterMenu : GxWinPopup_LoseFocusDisable
	{
        [SerializeField] private UIText m_Title;
        [SerializeField] private UIButton m_LoadVRMA;

		private GxWinCharacter m_LinkedCharWin;
		public GxWinCharacter LinkedWin => m_LinkedCharWin;
		public GxCharacter Character => m_LinkedCharWin?.Character;

		private void Awake()
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
			//(var token, var popup) = UIPopup.Explorer(Application.streamingAssetsPath, new[] { ".vrma" }, OnVRMASelected);
			//m_Explorer = popup as UIExplorer;

			var pos = new Vector3(0f, 0f, -20f);
			var osPos = LinkedWin.dwCamera.GetMousePosInOSSpace();
			var osSize = new Vector2Int(300, 500);
			GxWinPopup.Explorer(pos, osPos, osSize, out var popup, out m_Explorer);
			if (m_Explorer != null)
			{
				var files = new[] { ".vrma" };
				var info = new SelectedData(LinkedWin.Character);
				m_Explorer.Init(Application.streamingAssetsPath, files, (path) =>
				{
					OnVRMASelected(path, info);
				});
			}
		}

		private struct SelectedData
		{
			public GxCharacter Character;
			public SelectedData(GxCharacter character)
			{
				Character = character;
			}
		}
		UIExplorer m_Explorer;
		private void OnVRMASelected(string path, SelectedData data)
		{
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("VRMA path cannot be null.", this);
				return;
			}
			var ch = Character == null ? data.Character : Character;
			if (ch == null)
			{
				Debug.LogError("Character is not initialized.");
				return;
			}

			// Play the VRMA animation
			ch.CrossFadeVRMA(path);

			// Auto close the explorer popup, after loading the VRMA
			//if (m_Explorer)
			//{
			//	m_Explorer.SelfDespawn();
			//	m_Explorer = null;
			//}
		}

		public override bool Initialized 
		{
			get
			{
				return m_LinkedCharWin != null && base.Initialized;
			}
		}
		public void Init(GxWinCharacter winChar)
        {
            this.m_LinkedCharWin = winChar;
            Debug.Assert(m_LinkedCharWin != null, "ModelView cannot be null");
			var _name = winChar.gameObject.name;

			this.gameObject.SetActive(true);
			this.gameObject.name = $"Menu: {_name}";

			if (m_Title != null)
            {
                m_Title.Text = $"Menu: {_name}";
			}
		}
	}
}