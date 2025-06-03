using DesktopWizard;
using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Gaia
{
    [System.Obsolete("replace DwOSBody", true)]
    public class DesktopGizmos : MonoBehaviour
    {
        [SerializeField] Color m_WinColor = Color.white;

        [SerializeField] bool m_IncludeMode1 = false;
        [SerializeField] WinStyle m_WinStyle = WinStyle.MINIMIZE | WinStyle.MAXIMIZE;

        [SerializeField] bool m_IncludeMode2 = false;
        [SerializeField] WinExStyle m_WinExStyle = WinExStyle.LAYERED | WinExStyle.NOREDIRECTIONBITMAP | WinExStyle.TOOLWINDOW;

        [SerializeField] Vector2 m_Offset = Vector2.zero;
        [SerializeField] bool m_Detail = false;

        [Header("Debug")]
        [SerializeField] bool m_Debug = false;
        [SerializeField] string m_WinTitleContain = "";
        [SerializeField] Vector2Int m_WinMoveBy;

        private void OnValidate()
        {
            if (m_Debug)
            {
                m_Debug = false;
                if (string.IsNullOrWhiteSpace(m_WinTitleContain))
                {
                    Debug.LogWarning("Invalid win title");
                    return;
                }
                foreach (var win in DwCore.GetVisibleNormalWindows(false))
                {
                    if (!win.title.Contains(m_WinTitleContain, System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    win.MoveBy(m_WinMoveBy);
                }
            }
        }

        private void OnDrawGizmos()
        {
            var wins = DwCore.GetWindowsByOrder();
            if (wins == null)
                return;
            foreach (var win in wins)
            {
                if (m_IncludeMode1)
                {
                    if ((win.style & m_WinStyle) == 0)
                        continue;
                }
                else
                {
                    if ((win.style & m_WinStyle) != 0)
                        continue;
                }

                if (m_IncludeMode2)
                {
                    if ((win.exStyle & m_WinExStyle) == 0)
                        continue;
                }
                else
                {
                    if ((win.exStyle & m_WinExStyle) != 0)
                        continue;
                }
                var lt = win.rect.GetCorners()[1];
                GizmosExtend.DrawLabel((Vector3)lt, $"{win.ToString(m_Detail)}", m_Offset.x, m_Offset.y);
                win.rect.GizmosDraw(m_WinColor);
            }
        }
    }
}