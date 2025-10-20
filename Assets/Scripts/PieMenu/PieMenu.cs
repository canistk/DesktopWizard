using Gaia;
using Kit2.ObjectPool;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2.PieMenu
{
    [ExecuteInEditMode]
    public class PieMenu : MonoBehaviour
    {
		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(1, 10)]
		int m_MenuItemCount = 1;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(0, 100)]
		float m_MenuItemSpacing = 0f;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(0, 360)]
		float m_RotationOffset = 0f;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(100f, 1000)]
		float m_Size = 100f;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField]
		bool m_IconEnable = false;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(0f, 1f)]
		float m_IconSlerp = 0.5f;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(0, 500)]
		float m_IconDistance = 100f;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(50, 500)]
		float m_IconSize = 50f;

		[Header("Menu Item")]
		[SerializeField] PieMenuItem m_PieMenuItemPrefab = null;
		[SerializeField] RectTransform m_PivotTransform = null;

		[Header("UIs")]
		[SerializeField] UIText m_Title = null;
		[SerializeField] UIText m_Description = null;

#if UNITY_EDITOR
		private void OnEnable()
		{
			UnityEditor.AssemblyReloadEvents.afterAssemblyReload += Editor_Recompile;
		}

		private void OnDisable()
		{
			UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= Editor_Recompile;
		}
#endif
		private void Start()
		{
			InitializePieMenu();
		}

		private void OnDestroy()
		{
			CleanTokens();
		}

		private void InitializePieMenu()
		{
			if (GameObjectExtend.IsInPrefabIsolationMode())
			{
				Debug.LogWarning("PieMenu abort initialize due to Prefab mode.");
				return;
			}

			//InitializeComponents();
			//ReadDataAndSetPieMenuInfoFields();

			//OnComponentsInitialized?.Invoke();
			//OnPieMenuFullyInitialized?.Invoke();

			//if (gameObject.activeSelf && Application.isPlaying)
			//{
			//	PieMenuShared.References.PieMenuToggler.SetActive(this, true);
			//}
			Editor_OnMenuSettingChanged();
		}

		private void Editor_Recompile()
		{
			CleanTokens();
			InitializePieMenu();
		}

		private void Editor_OnMenuSettingChanged()
		{
			HandleItemAmount();
			HandleItemsSetup();
		}


		#region Spawn Control
		private KxObjectPool m_Pool;
		private KxObjectPool Pool
		{
			get
			{
				if (m_Pool == null)
					m_Pool = this.GetOrAddComponent<KxObjectPool>();
				return m_Pool;
			}
		}

		private RectTransform PivotTransform
		{
			get
			{
				if (m_PivotTransform == null)
				{
					var go = new GameObject("Pivot", typeof(RectTransform));
					go.transform.SetParent(transform, false);
					m_PivotTransform = go.transform as RectTransform;
					#if UNITY_EDITOR
					if (this.IsEditorMode())
						UnityEditor.EditorUtility.SetDirty(go);
					#endif
				}
				return m_PivotTransform;
			}
		}

		private List<PieMenuItem> m_Items = new List<PieMenuItem>();

		private PieMenuItem AddItem()
		{
			Debug.Assert(m_PieMenuItemPrefab != null, $"{nameof(m_PieMenuItemPrefab)} are required.");
			GameObject token = null;
			if (this.IsEditorMode())
			{
				token = GameObject.Instantiate(m_PieMenuItemPrefab.gameObject, PivotTransform);
				token.hideFlags = HideFlags.DontSave;
				token.name = $"{m_PieMenuItemPrefab.name}(Preview)";
			}
			else
			{
				token = Pool.Spawn(m_PieMenuItemPrefab.gameObject, PivotTransform, false);
			}
			token.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			var comp = token.GetComponent<PieMenuItem>();
			m_Items.Add(comp);
			return comp;
		}

		private void RemoveItemAt(int index)
		{
			if (index < 0 || index >= m_Items.Count)
				throw new System.IndexOutOfRangeException();
			if (this.IsEditorMode())
			{
				if (m_Items[index] != null)
					GameObject.DestroyImmediate(m_Items[index].gameObject);
			}
			else
			{
				var token = m_Items[index].gameObject;
				Pool.Despawn(token);
			}
			m_Items.RemoveAt(index);
		}

		private void RemoveLast()
		{
			if (m_Items.Count == 0)
				return;
			RemoveItemAt(m_Items.Count - 1);
		}

		private void CleanTokens()
		{
			var cnt = m_Items.Count;
			while (cnt-- > 0)
			{
				RemoveItemAt(cnt);
			}
		}

		#endregion Spawn Control

		private void HandleItemAmount()
		{
			if (m_PieMenuItemPrefab == null)
			{
				Debug.LogWarning($"{nameof(m_PieMenuItemPrefab)} are required.");
				return;
			}

			var amount = m_MenuItemCount;
			var exist = m_Items.Count;
			if (amount < exist)
			{
				var diff = exist - amount;
				for (int i = 0; i < diff; ++i)
					RemoveLast();
			}
			if (amount > exist)
			{
				var diff = amount - exist;
				for (int i = 0; i < diff; ++i)
					AddItem();
			}
		}

		private void HandleItemsSetup()
		{
			var menuItemCount = m_Items.Count;
			var menuItemSpacing = m_MenuItemSpacing;
			var rotationOffset = m_RotationOffset;
			var size = m_Size;

			if (menuItemCount <= 0)
			{
				Debug.LogError("Invalid, no item exist");
				return;
			}

			const float CIRCLE = 360f;
			var fCnt		= (float)menuItemCount;
			var totalGap	= menuItemCount <= 1 ? 0 : fCnt * menuItemSpacing; // no gap when it's the only one.
			var fillArea	= CIRCLE - totalGap;
			if (fillArea <= 0f)
			{
				Debug.LogError("Not enough space to generate parts.", this);
				return;
			}

			var perAngle	= fillArea / fCnt;
			var accAngle	= rotationOffset;
			var slerp		= m_IconSlerp;
			var iconDis		= m_IconDistance;
			for (int i = 0; i < menuItemCount; ++i)
			{
				var item = m_Items[i];
				item.Editor_Preview(accAngle, perAngle, size);
				item.SetIconParams(iconDis, slerp, m_IconSize);
				//item.SetIcon(null);
				accAngle += perAngle + menuItemSpacing; // next angle
			}

		}

		public void SetItems(params ButtonData[] data)
		{
			SetItems(null, data);
		}

		public void SetItems(System.Action<ButtonData> overrideAction, params ButtonData[] data)
		{
			m_MenuItemCount = data.Length;
			// m_MenuItemSpacing
			//m_RotationOffset
			//m_Size
			HandleItemAmount();
			HandleItemsSetup();
			var cnt = m_Items.Count;
			for (int i = 0; i < cnt; ++i)
			{
				var item = m_Items[i];
				var d = data[i];
				if (d.callback == null && overrideAction != null)
					d.callback = overrideAction;
				item.SetIcon(d.icon);
				item.SetCallback(OnItemEnter, OnItemExit, OnItemClicked);
				item.SetData(d);
			}
		}

		private void OnItemEnter(PieMenuItem item)
		{
			var data = item.GetData();
			SetDescription(data);
		}

		private void OnItemExit(PieMenuItem item)
		{
			CleanDescription();
		}

		private void OnItemClicked(PieMenuItem item)
		{
			var data = item.GetData();
			if (data?.callback == null)
				return;
			data.callback.TryCatchDispatchEventError(o => o?.Invoke(data));
		}

		private void SetDescription(ButtonData data)
		{
			if (m_Title)
				m_Title.Text = data.name;
			if (m_Description)
				m_Description.Text = data.description;
		}
		private void CleanDescription()
		{
			if (m_Title)
				m_Title.Text = string.Empty;
			if (m_Description)
				m_Description.Text = string.Empty;
		}

		[SerializeField]
		private ButtonData[] m_Test = new ButtonData[]
		{
			new ButtonData("item 01", "Item 01 Desc", null),
			new ButtonData("item 02", "Item 02 Desc", null),
			new ButtonData("item 03", "Item 03 Desc", null),
			new ButtonData("item 04", "Item 04 Desc", null),
			new ButtonData("item 05", "Item 05 Desc", null),

		};
		[ContextMenu("Test01")]
		private void Test01()
		{
			SetItems(BtnClick, m_Test);
			void BtnClick(ButtonData d)
			{
				Debug.Log($"{d.name} clicked");
			}
		}
	}

	[System.Serializable]
	public class ButtonData
	{
		public string name;
		public string description;
		public Sprite icon;
		public System.Action<ButtonData> callback;
		public ButtonData(string name, string description, Sprite icon = null, System.Action<ButtonData> callback = null)
		{
			this.name = name;
			this.description = description;
			this.icon = icon;
			this.callback = callback;
		}
		public ButtonData(string name, string description, System.Action<ButtonData> callback)
			: this(name, description, null, callback) {}
	}
}