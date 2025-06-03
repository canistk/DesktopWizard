using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Kit2;
using Kit2.Task;
namespace Gaia
{
	public class HeroineImportProcessor : AssetPostprocessor
	{
		private const string s_JxDirectory = "Assets/Jx/Heroine/";
		private const string s_HeroineDirectory = "Assets/Gaia/Heroine/";
		private const string s_HeroineFileName = "Heroine";
		private const string s_HairFileName = "HairA1";
		private System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
		private static bool IsAssetInFolder(string assetPath, string targetFolder)
		{
			if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(targetFolder))
				return false;

			EditorExtend.ResolvePath(assetPath, out string absolutePath0, out string relativePath0);
			EditorExtend.ResolvePath(targetFolder, out string absolutePath1, out string relativePath1);
			//Debug.Log(assetPath);
			//Debug.Log("Relative 0:" + relativePath0);
			//Debug.Log("Relative 1:" + relativePath1);
			var rst = relativePath0.StartsWith(relativePath1, System.StringComparison.InvariantCultureIgnoreCase);
			return rst;
		}

		void OnPostprocessModel(GameObject model)
		{
			var assetPath = assetImporter.assetPath;
			if (IsAssetInFolder(assetPath, s_HeroineDirectory))
			{
				var fileName = Path.GetFileNameWithoutExtension(assetPath);
				if (!fileName.Equals(s_HeroineFileName, IGNORE))
					return;

				// all matched, is our target file.
				Debug.Log($"Asset Heroine import detected.\nPath = {assetPath}");
				var task = new WaitForFBXImportCompleted(assetPath, (imported) =>
				{
					OnFBXImportedCompleted(assetPath, imported);
				}, (ex) =>
				{
					Debug.LogError($"Heroine import flow failed\nPath = {assetPath}");
					EditorUtility.DisplayDialog("Heroine", "Heroine import flow failed", "OK");
				});
				MyEditorTaskHandler.Add(task);
			}

			if (IsAssetInFolder(assetPath, s_JxDirectory))
			{
				var fileName = Path.GetFileNameWithoutExtension(assetPath);
				if (fileName.Equals(s_HeroineFileName, IGNORE))
				{
					// all matched, is our target file.
					Debug.Log($"Asset Heroine import detected.\nPath = {assetPath}");
					var task = new WaitForFBXImportCompleted(assetPath, (imported) =>
					{
						OnJxHeroineFBXImportedCompleted(s_JxDirectory, imported);
					}, (ex) =>
					{
						Debug.LogError($"Heroine import flow failed\nPath = {assetPath}");
						EditorUtility.DisplayDialog("Heroine", "Heroine import flow failed", "OK");
					});
					MyEditorTaskHandler.Add(task);
				}
				else if (fileName.Equals(s_HairFileName, IGNORE))
				{
					Debug.Log($"Asset Hair import detected.\nPath = {assetPath}");
					var task = new WaitForFBXImportCompleted(assetPath, (imported) =>
					{
						OnJxHairFBXImportedCompleted(s_JxDirectory, imported);
					}, (ex) =>
					{
						Debug.LogError($"Heroine import flow failed\nPath = {assetPath}");
						EditorUtility.DisplayDialog("Heroine_HairA1", "Heroine import flow failed", "OK");
					});
					MyEditorTaskHandler.Add(task);
				}
			}
		}

		private static void OnFBXImportedCompleted(string assetPath, GameObject fbx)
		{
			Debug.Log($"Heroine import flow initialize.\nPath = {assetPath}\nFBX = {fbx}");
			MyEditorTaskHandler.Add(new CreateHeroineBody(assetPath, fbx));
			MyEditorTaskHandler.Add(new CreateHeroineHair(assetPath, fbx));
			MyEditorTaskHandler.Add(new CreateHeroineTShirt(assetPath, fbx));
			MyEditorTaskHandler.Add(new CreateHeroineUnder(assetPath, fbx));
		}

		private static void OnJxHairFBXImportedCompleted(string assetPath, GameObject fbx)
		{
			Debug.Log($"Heroine import flow initialize.\nPath = {assetPath}\nFBX = {fbx}");
			MyEditorTaskHandler.Add(new CreateJxHeroineHair(assetPath, fbx));
		}

		private static void OnJxHeroineFBXImportedCompleted(string assetPath, GameObject fbx)
		{
			Debug.Log($"Heroine import flow initialize.\nPath = {assetPath}\nFBX = {fbx}");
			MyEditorTaskHandler.Add(new CreateJxHeroineBody(assetPath, fbx));
		}

	}
}