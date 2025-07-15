using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniGLTF;
using UniVRM10;
using System.Threading.Tasks;
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

		public static bool UnloadModel(GxCharacter ch)
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
				// ISSUE : 
				// step 0 : load VRM,
				// step 1 : right click on VRM (GxWinPopup menu)
				// step 2 : load another VRM (fail, no exception detected)
				// not an issue _ Dig Vrm10.cs > Line 191, TryLoadingAsVrm10Async will pause without exception.
				// the awaitCaller will be hang for no reason.
#if false
				var vrm = await Vrm10.LoadPathAsync(path, true);
#else
				byte[] bytes = null;
				using (FileStream fs = File.OpenRead(path))
				{
					using (BinaryReader binaryReader = new BinaryReader(fs))
					{
						bytes = binaryReader.ReadBytes((int)fs.Length);
					}
				}

				const float TIMEOUT = 10f;

				IAwaitCaller awaitCaller = Application.isPlaying
					? new RuntimeOnlyAwaitCaller(TIMEOUT)
					: new ImmediateCaller();

				// attempt 1
				// var _gltfData = new GlbLowLevelParser(string.Empty, bytes).Parse();

				/*
				// attempt 2
				// remark : parser isn't the issue. AwaitCaller will not response correctly.
				// will hang on next awaitCaller.
				using var oper = awaitCaller.Run(() => new GlbLowLevelParser(string.Empty, bytes).Parse());
				while (!oper.IsCompleted)
				{
					await Task.Yield();
					//await awaitCaller.NextFrameIfTimedOut();
				}
				if (oper.IsCanceled)
				{
					Debug.LogError($"VRM loading canceled: {path}");
					return;
				}
				if (oper.IsFaulted || oper.Exception != null)
				{
					if (fail != null)
						fail.Invoke(oper.Exception);
					return;
				}
				//*/

				//*
				// attempt 3
				var _gltfData = await Task.Run(() =>
				{
					try
					{
						var rst = new GlbLowLevelParser(string.Empty, bytes).Parse();
						return rst;
					}
					catch (System.Exception ex)
					{
						Debug.LogException(ex);
						return null;
					}
				});
				//*/

				Vrm10Instance vrm = null;
				try
				{
					using (var gltfData = _gltfData)
					{
						vrm = await Vrm10.LoadGltfDataAsync(gltfData,
							canLoadVrm0X: true, // allow load vrm0x
							controlRigGenerationOption: ControlRigGenerationOption.Generate,
							showMeshes: true,
							awaitCaller: awaitCaller,
							textureDeserializer: null,
							materialGenerator: null,
							vrmMetaInformationCallback: null,
							ct: default,
							importerContextSettings: null
						// springboneRuntime: null
						);
					}
				}
				catch (System.Exception ex)
				{
					throw ex;
				}
				finally
				{
					Debug.Log("should be Loaded");
				}
#endif

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