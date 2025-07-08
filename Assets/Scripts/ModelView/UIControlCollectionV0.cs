using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kit2;
using Kit2.ObjectPool;
namespace Gaia
{
	public interface ISortFilter
	{
		public void SetFilter(System.Func<object, bool> filteringMethod);
		public void SetSorting(System.Func<object, object, int> sortingMethod);
		public void SetOrder(System.Func<object, int> orderMethod);
		public void Refresh();
	}

	[RequireComponent(typeof(KxObjectPool))]
	public class UIControlCollectionV0 : UISpawnRoot, ISortFilter
	{
		[Tooltip("Fetch Binging from children of prefab")]
		[SerializeField] bool m_GetComponentsInChildren = true;
		[SerializeField] bool m_ResetTokenSize_Scale1 = true;
		public Transform m_SpawnLayer;
		private Dictionary<BindingBase, GameObject /*prefab*/> _prefabDict = null;
		private Dictionary<BindingBase, GameObject /*prefab*/> prefabDict
		{
			get
			{
				if (_prefabDict is null)
				{
					_prefabDict = new();
					foreach (var prefab in pool.prefabs)
					{
						if (prefab == null)
							continue;

						var ctrls = m_GetComponentsInChildren ?
							prefab.GetComponentsInChildren<BindingBase>() : FetchCompSingle<BindingBase>();
						T[] FetchCompSingle<T>()
						{
							var tmp = prefab.GetComponent<T>();
							return tmp is null ?
								new T[0] :
								new T[] { tmp };
						}

						if (ctrls == null || ctrls.Length <= 0)
						{
							Debug.LogError($"fail to GetComponent<{nameof(BindingBase)}> in prefab {prefab.name}", prefab);
							continue;
						}

						foreach (var c in ctrls)
						{
							_prefabDict.Add(c, prefab);
						}
					}
				}
				return _prefabDict;
			}
		}

		private KxObjectPool m_Pool = null;
		private KxObjectPool pool
		{
			get
			{
				if (m_Pool == null)
					m_Pool = GetComponent<KxObjectPool>();
				return m_Pool;
			}
		}

		public bool CanHandle<T>(T data)
			where T : class
		{
			foreach (var ctrl in prefabDict.Keys)
			{
				if (ctrl.CanHandle(data))
					return true;
			}
			return false;
		}

		private bool TryGetValidPrefab(object data, out GameObject prefab)
		{
			foreach ((var bind, var _prefab) in prefabDict)
			{
				if (!bind.CanHandle(data))
					continue;

				prefab = _prefab;
				return prefab != null;
			}
			prefab = null;
			return false;
		}

		public delegate void AfterUISpawnDelegate(BindingBase bindingUI, object data);
		public AfterUISpawnDelegate m_AfterUISpawnCallback;
		public delegate void AfterUIDespawnDelegate(BindingBase bindingUI);
		public AfterUIDespawnDelegate m_AfterUIDespawnCallback;

		public void SpawnByDataList<T>(IEnumerable<T> data,
			AfterUISpawnDelegate afterUISpawnCallback = null,
			AfterUIDespawnDelegate afterUIDespawnCallback = null)
			where T : class
		{
			if (m_Cmd == null)
			{
				m_Cmd = new SpawnCommand(this, DefineSpawnMethod, DefineDespawnMethod);
				m_Cmd.SetSpawnDesc(new UISpawnCommand<object, BindingBase, KxObjectPool>.SpawnDesc
				{
					pool = pool,
					root = m_SpawnLayer,
					prefab = null, /// <see cref="TryGetValidPrefab(object, out GameObject)"/>
				});
				m_Cmd.SetSpawnCallback(InternalAfterVisible);
				m_Cmd.SetDespawnCallback(InternalAfterInVisible);
			}
			else
			{
				m_Cmd.ClearAllSpawnedUI();
			}

			this.m_AfterUISpawnCallback = afterUISpawnCallback;
			this.m_AfterUIDespawnCallback = afterUIDespawnCallback;
			if (m_Cmd == null)
				throw new System.Exception("Invalid spawn flow.");
			this.m_dataCache = data;

			InternalReflesh();
		}

		public void DespawnAll<T>() where T : class
		{
			SpawnByDataList(Enumerable.Empty<T>());
		}

		private bool IsDataReady => m_Cmd != null && m_dataCache != null;
		private IEnumerable<object> m_dataCache;
		private void InternalReflesh()
		{
			if (m_Cmd == null)
				throw new System.Exception("Invalid spawn flow.");
			if (m_dataCache == null)
				throw new System.Exception("data == null.");

			var processedData = m_dataCache.Where(InternalFilter).ToList();

			if (sortingMethod != null)
				processedData.Sort(InternalSorting);

			if (orderMethod != null)
				processedData = processedData.OrderBy(InternalOrder).ToList();

			m_Cmd.Reset(processedData);
			m_Cmd.ClearAllSpawnedUI();
			this.Spawn(m_Cmd);
		}
		public void Refresh()
		{
			if (!IsDataReady)
				return;
			InternalReflesh();
		}

		private BindingBase DefineSpawnMethod(object data)
		{
			// locate suitable prefab based on input data type.
			if (!TryGetValidPrefab(data, out var prefab))
			{
				Debug.LogError($"Prefab on the list, fail to handle '{data}', please check {nameof(prefabDict)} reference.");
				return null;
			}

			var spawnToken = pool.Spawn(prefab, m_SpawnLayer);
			if (spawnToken == null)
				return null;

			if (m_ResetTokenSize_Scale1 && spawnToken)
				spawnToken.transform.localScale = Vector3.one;

			// Filter binding based on input data.
			BindingBase found = null;
			var comps = spawnToken.GetComponents<BindingBase>();
			for (int i = 0; i < comps.Length && found == null; ++i)
			{
				if (!comps[i].CanHandle(data))
					continue;
				found = comps[i];
			}
			return found;
		}

		private void DefineDespawnMethod(BindingBase binding)
		{
			if (!pool.IsSpawned(binding.gameObject))
				return;
			pool.Despawn(binding.gameObject);
		}

		private void InternalAfterVisible(BindingBase binding, object data)
		{
			if (binding == null)
			{
				Debug.LogError("Missing required component on prefab.");
				return;
			}
			if (data == null)
			{
				Debug.LogError("Invalid data = null");
				return;
			}
			binding.Assign(data);

			this.m_AfterUISpawnCallback?.TryCatchDispatchEventError(o => o?.Invoke(binding, data));
		}

		private void InternalAfterInVisible(BindingBase binding)
		{
			this.m_AfterUIDespawnCallback?.TryCatchDispatchEventError(o => o?.Invoke(binding));
		}

		protected override void Editor_ReplaceScrollRect()
		{
			base.Editor_ReplaceScrollRect();
			if (scrollRect != null)
			{
				m_SpawnLayer = scrollRect.content;
			}
		}

		#region Filter
		private System.Func<object, bool> m_filteringMethod = null;
		private bool InternalFilter(object obj)
		{
			if (m_filteringMethod == null)
				return true;
			return m_filteringMethod.Invoke(obj);
		}
		public void SetFilter(System.Func<object, bool> filteringMethod)
		{
			this.m_filteringMethod = filteringMethod;
			if (IsDataReady)
				InternalReflesh();
		}
		public void ClearFilter()
		{
			this.m_filteringMethod = null;
			if (IsDataReady)
				InternalReflesh();
		}
		#endregion Filter

		#region Sorting
		private System.Func<object, object, int> sortingMethod = null;
		private int InternalSorting(object a, object b)
		{
			if (sortingMethod == null)
				return 0;
			return sortingMethod(a, b);
		}
		public void SetSorting(System.Func<object, object, int> sortingMethod)
		{
			this.sortingMethod = sortingMethod;
			if (IsDataReady)
				InternalReflesh();
		}
		public void ClearSorting()
		{
			sortingMethod = null;
			if (IsDataReady)
				InternalReflesh();
		}
		#endregion Sorting

		#region Order
		private System.Func<object, int> orderMethod = null;
		private int InternalOrder(object obj)
		{
			if (orderMethod == null)
				return 0;
			return orderMethod(obj);
		}
		public void SetOrder(System.Func<object, int> orderMethod)
		{
			this.orderMethod = orderMethod;
			if (IsDataReady)
				InternalReflesh();
		}
		public void ClearOrder()
		{
			orderMethod = null;
		}
		#endregion Order

		#region Spawn Command
		private SpawnCommand m_Cmd = null;

		private class SpawnCommand : UISpawnCommand<object, BindingBase, KxObjectPool>
		{
			private readonly UIControlCollectionV0 collection;
			private System.Func<object, BindingBase> how2SpawnCallback;
			private System.Action<BindingBase> how2DespawnCallback;
			public SpawnCommand(UIControlCollectionV0 collection,
				System.Func<object, BindingBase> how2SpawnCallback,
				System.Action<BindingBase> how2DespawnCallback)
			{
				this.collection = collection;
				this.how2SpawnCallback = how2SpawnCallback;
				this.how2DespawnCallback = how2DespawnCallback;
			}

			public override bool CheckValidSpawn(in SpawnDesc desc)
			{
				if (desc.pool == null || desc.root == null)
				{
					Debug.LogError($"{GetType().Name}.SpawnDesc : Invalid");
					return false;
				}
				return true;
			}

			protected override BindingBase OnSpawnUI(object data)
			{
				// override base logic.
				return how2SpawnCallback(data);
			}

			protected override void OnDespawnUI(BindingBase ui)
			{
				how2DespawnCallback(ui);
			}
		}
		#endregion Spawn Command
	}
}