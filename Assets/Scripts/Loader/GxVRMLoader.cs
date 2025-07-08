using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniGLTF;
using UnityEngine;
using UniVRM10;
namespace Gaia
{
    public static class GxVRMLoader
    {
		#region Charcter
		public static void LoadModel(string path,
			System.Action<GxCharacter, Vrm10Instance> success,
			System.Action<System.Exception> fail = null)
		{
			InternalLoadModel(path, success, fail);
		}

		public static bool Unload(GxCharacter ch)
		{
			if (!s_Characters.Contains(ch))
				return false;
			s_Characters.Remove(ch);
			GameObject.Destroy(ch.gameObject);
			return true;
		}

		private static List<GxCharacter> s_Characters = new List<GxCharacter>();

		private static async void InternalLoadModel(string path,
			System.Action<GxCharacter, Vrm10Instance> success,
			System.Action<System.Exception> fail = null)
		{
			try
			{
				if (string.IsNullOrEmpty(path))
				{
					Debug.LogError("VRM path is null or empty.");
					return;
				}
				var fileInfo = new FileInfo(path);
				if (!fileInfo.Exists)
				{
					Debug.LogError($"Invaild File {path}");
					return;
				}

				// Loaded VRM model
				var vrm = await Vrm10.LoadPathAsync(path, true);

				if (vrm == null)
					throw new System.NullReferenceException($"Fail to load VRM at path {path}");

				// post loaded for character setup, Get the character component
				var character = vrm.gameObject.AddComponent<GxCharacter>();
				character.RuntimeCreation();
				s_Characters.Add(character);

				success?.Invoke(character, vrm);
			}
			catch (System.Exception ex)
			{
				if (fail == null)
				{
					Debug.LogException(ex);
					return;
				}
				fail.Invoke(ex);
			}
		}

		#endregion Charcter
	}
}