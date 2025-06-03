using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DesktopWizard;
namespace BehaviorDesigner.Runtime
{
    [System.Serializable]
    public class SharedDwCamera : SharedVariable<DwCamera>
    {
        public static implicit operator SharedDwCamera(DwCamera value)
            => new SharedDwCamera { mValue = value };
    }
}