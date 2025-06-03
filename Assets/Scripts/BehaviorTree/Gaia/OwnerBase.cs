using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public abstract class OwnerBase : ActionBase
    {
        protected HeroineCore m_Core;
		protected HeroineCore Core
		{
			get
			{
				if (m_Core == null)
				{
					m_Core = gameObject.GetComponent<HeroineCore>();
				}
				return m_Core;
			}
		}
	}
}