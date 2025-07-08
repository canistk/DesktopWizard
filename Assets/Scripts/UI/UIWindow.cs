using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Gaia
{
    public class UIWindow : MonoBehaviour
    {
        [SerializeField] UIText m_Title;
        public string Title
        {
            get => m_Title.Text;
            set => m_Title.Text = value;
        }

        [SerializeField] RectTransform m_Content;
        public RectTransform Content
        {
            get => m_Content;
        }

        [SerializeField] ScrollRect m_ScrollRect;
        public ScrollRect scrollRect => m_ScrollRect;
    }
}