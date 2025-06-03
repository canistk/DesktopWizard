using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public abstract class GxUIPrevNextBase<T> : GxUISetting<T>
	{
		[SerializeField] UIButton m_PrevBtn, m_NextBtn;
		[SerializeField] UIText m_Label;

		protected void Start()
		{
			Init();
		}

		private KeyValuePair<T, string>[] m_Kvp;
		private void Init()
		{
			m_PrevBtn.EVENT_OnClick += OnPrevClicked;
			m_NextBtn.EVENT_OnClick += OnNextClicked;

			var selected = GetIndex();
			m_Label.Text = cache[selected].Value;
		}

		private void OnPrevClicked()
		{
			var i = GetIndex();
			if (--i < 0)
				i = cache.Length - 1;
			SetIndex(i);
		}

		private void OnNextClicked()
		{
			var i = GetIndex();
			if (++i >= cache.Length)
				i = 0;
			SetIndex(i);
		}

		protected override void OnSourceUpdated(int index, string name, T value)
		{
			m_Label.Text = cache[index].Value;
		}
	}
}