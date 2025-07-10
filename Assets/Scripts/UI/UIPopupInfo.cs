using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class UIPopupInfo : UIPopupBase
	{
		[SerializeField] UIText m_Title;
		[SerializeField] UIText m_Content;
		[SerializeField] UIButton m_ConfimBtn;

		private System.Action m_Callback;

		public void Init(string title, string content, string confirmbtn, System.Action callback)
		{
  			if (m_Title)
				m_Title.Text = title;
			if (m_Content)
				m_Content.Text = content;
			this.m_Callback = callback;
			if (m_ConfimBtn)
			{
				m_ConfimBtn.Label = confirmbtn;
				m_ConfimBtn.EVENT_OnClick -= OnConfirm;
				m_ConfimBtn.EVENT_OnClick += OnConfirm;
			}
		}

		private void OnConfirm()
		{
			if (m_Callback == null)
				return;
			m_Callback.Invoke();
			m_Callback = null;
			SelfDespawn();
		}
	}
}