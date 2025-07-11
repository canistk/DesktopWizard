using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
using Kit2.ObjectPool;
namespace Gaia
{
    public static class UIPopup
    {
		private struct AssetPath
		{
			public string path;
			public bool isAddressable;
			public AssetPath(string path, bool isAddressable)
			{
				this.path = path;
				this.isAddressable = isAddressable;
			}
		}
		private static Dictionary<System.Type, AssetPath> s_Type2Path = null;
		private static Dictionary<System.Type, AssetPath> type2Path
		{
			get
			{
				if (s_Type2Path != null)
					return s_Type2Path;

				s_Type2Path = new Dictionary<System.Type, AssetPath>
				{
					{ typeof(UIPopupInfo),              new AssetPath("UIs/UIPopupInfo", false) },
					{ typeof(UIExplorer),               new AssetPath("UIs/UIExplorer", false) },
					{ typeof(UIPopupCharacterMenu),		new AssetPath("UIs/UIPopupCharacterMenu", false) },
				};
				return s_Type2Path;
			}
		}

		private static (GameObject token, TYPE popup) InternalSpawn<TYPE>()
			where TYPE : Component
		{
			var t = typeof(TYPE);
			if (!type2Path.TryGetValue(t, out var o))
				throw new System.Exception($"Prefab for {t} not exist.");
			
			var src = o.isAddressable ? eSrcType.Addressable : eSrcType.Resources;
			return UIPopupCanvas.Instance.Spawn<TYPE>(o.path, src);
		}

		public static (GameObject token, UIPopupInfo popup) Info(string title, string content, string confirm = "Confirm", System.Action callback = null)
		{
			(var token, var comp) = InternalSpawn<UIPopupInfo>();
			if (comp == null)
			{
				Debug.LogError($"Fail to spawn UIPopupInfo, prefab path: {type2Path[typeof(UIPopupInfo)].path}");
				return (null, null);
			}

			comp.Init(title, content, confirm, callback);
			return (token, comp);

		}

		public static (GameObject token, UIExplorer popup) Explorer(string path, string[] ext, System.Action<string> fileSelected)
		{
			(var token, var comp) = InternalSpawn<UIExplorer>();
			if (comp == null)
			{
				Debug.LogError($"Fail to spawn UIExplorer, prefab path: {type2Path[typeof(UIExplorer)].path}");
				return (null, null);
			}
			comp.Init(path, ext, fileSelected);
			return (token, comp);
		}

		[System.Obsolete("move to GxModelView.DisplayCharacterMenu")]
		public static (GameObject token, UIPopupCharacterMenu popup) CharacterMenu(GxModelView modelView, GxCharacter character)
		{
			// TODO: this shouldn't bind with UIPopup, since we want ModelView to hand over. 
			(var token, var comp) = InternalSpawn<UIPopupCharacterMenu>();
			if (comp == null)
			{
				Debug.LogError($"Fail to spawn UIPopupCharacterMenu, prefab path: {type2Path[typeof(UIPopupCharacterMenu)].path}");
				return (null, null);
			}
			comp.Init(modelView, character);
			return (token, comp);
		}
	}
}