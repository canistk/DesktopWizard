using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePieMenu;
namespace Gaia
{
    [RequireComponent(typeof(PieMenu))]
    public class UIPieMenu : MonoBehaviour
    {
        [SerializeField] PieMenu m_Source;
        private PieMenu Source
        {
            get
            {
                if (m_Source == null)
                {
                    m_Source = GetComponent<PieMenu>();
                }
                return m_Source;
            }
        }

        public void Show()
        {
            if (Source == null)
                throw new System.NullReferenceException();
            var menu = Source.PieMenuInfo;
            if (menu != null && !menu.IsActive && !menu.IsTransitioning)
                PieMenuShared.References.PieMenuToggler.SetActive(Source, true);
        }
    }
}