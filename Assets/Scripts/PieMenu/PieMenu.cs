using Kit2.ObjectPool;
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
		int m_MenuItemCount;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(0, 100)]
		float m_MenuItemSpacing;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(0, 360)]
		float m_RotationOffset;

		[Kit2.OnValueChange(nameof(Editor_OnMenuSettingChanged))]
		[SerializeField, Range(250, 1000)]
		float m_Size;

		[SerializeField] PieMenuItem m_PieMenuItemPrefab = null;

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

		[SerializeField] private RectTransform m_PivotTransform;
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
			for (int i = 0; i < menuItemCount; ++i)
			{
				var item = m_Items[i];
				var rot = Quaternion.Euler(0f, 0f, accAngle);
				item.transform.rotation = rot;
				item.SetAngle(perAngle);
				item.SetSize(size);
				accAngle += perAngle + menuItemSpacing; // next angle
			}

		}
	}
}