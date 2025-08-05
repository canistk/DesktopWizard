using Kit2;
using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Gaia
{
    public class WinPopupCharacterMenu : GxWinPopupContent
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
			_ = OpenExplorer();
		}
		private async Task OpenExplorer()
		{
			var pos = new Vector3(0f, 0f, -20f);
			var osPos = LinkedWin.dwCamera.GetMousePosInOSSpace();
			var osSize = new Vector2Int(300, 500);
			var info = new SelectedData(LinkedWin.Character);
			(var popup, var explorer) = await GxWinPopup.Explorer(
				pos, osPos, osSize,
				GxConst.Path.VRM, ".vrma",
				(path) =>
				{
					OnVRMASelected(path, info);
				},
				autoClose: true);
		}

		private struct SelectedData
		{
			public GxCharacter Character;
			public SelectedData(GxCharacter character)
			{
				Character = character;
			}
		}
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
			path = KxPath.Fix(path);
			var key = new GxMotionKey(path, eAssetType.VRMA);
			ch.CrossFade(key, 0f);
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