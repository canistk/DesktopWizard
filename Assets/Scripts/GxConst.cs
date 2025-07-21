using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public static class GxConst
    {
        public static class Path
        {
            private static readonly string Root = Application.persistentDataPath;

            public static readonly string VRM = KxPath.Combine(Root, "VRM");

            public static readonly string MotionDatabase = KxPath.Combine(Root, "MotionDatabase.json");
		}


		public static class Cmd
		{
			private const string EXPLORER = "explorer.exe";
			public static void OpenVRMFolder() => Platform.CommandLine(EXPLORER, GxConst.Path.VRM);
            public static void OpenStreamingAssets() => Platform.CommandLine(EXPLORER, KxPath.Fix(Application.streamingAssetsPath));
		}
	}
}