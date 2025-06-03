using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public static class MyGameSetting
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Debug.Log("MyGameSetting.Init()");
        }
    }
}