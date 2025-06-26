using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public abstract class OwnerBase : ActionBase
    {
        protected GxModelView m_ModelView;
		protected GxModelView ModelView
		{
			get
			{
				if (m_ModelView == null)
				{
					m_ModelView = gameObject.GetComponent<GxModelView>();
				}
				return m_ModelView;
			}
		}
	}
}