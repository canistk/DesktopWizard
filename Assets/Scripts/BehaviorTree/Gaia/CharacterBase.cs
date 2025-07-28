using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{

	public abstract class CharacterBase : WinBase
	{
		private KeyValuePair<bool, GxCharacter> m_Character;
		protected GxCharacter Character
		{
			get
			{
				if (!m_Character.Key)
				{
					if (ModelView is GxWinCharacter winCharacter && winCharacter.Character != null)
					{
						m_Character = new KeyValuePair<bool, GxCharacter>(true, winCharacter.Character);
						return m_Character.Value;
					}
					var comp = gameObject.GetComponentInChildren<GxCharacter>();
					m_Character = new KeyValuePair<bool, GxCharacter>(true, comp);
				}
				return m_Character.Value;
			}
		}
	}
}