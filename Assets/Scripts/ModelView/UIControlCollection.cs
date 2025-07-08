using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Gaia
{
    [RequireComponent(typeof(KxObjectPool))]
    public class UIControlCollection : MonoBehaviour
    {
        [SerializeField] KxObjectPool m_Pool;
		[SerializeField] bool m_SortByAlphabet = false;
		public KxObjectPool pool
		{
			get
			{
				if (m_Pool == null)
					m_Pool = GetComponent<KxObjectPool>();
				return m_Pool;
			}
		}
		
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

						var ctrl = prefab.GetComponentInChildren<BindingBase>();
						if (ctrl == null)
						{
							Debug.LogError($"fail to find {nameof(BindingBase)} in prefab");
							continue;
						}

						_prefabDict.Add(ctrl, prefab);
					}
				}
				return _prefabDict;
			}
		}

		//gameobjects here are assumed to be under the same parent
		private Dictionary<BindingBase, object> m_SpawnedDict = new Dictionary<BindingBase, object>();
		
		#region Filter
		private System.Func<object, bool> filteringMethod = null;

		/// <summary>
		/// defines when to turn off the spawned gameobject
		/// </summary>
		public void SetFilter(System.Func<object, bool> filteringMethod)
		{
			this.filteringMethod = filteringMethod;
			FilterAll();
		}

		private void FilterAll()
		{
			if (filteringMethod is null)
			{
				//no filtering method, show all
				foreach (var kvp in m_SpawnedDict)
				{
					kvp.Key.gameObject.SetActive(true);
				}
				return;
			}

			foreach (var kvp in m_SpawnedDict)
			{
				var control = kvp.Key;
				var data = kvp.Value;
				control.gameObject.SetActive(filteringMethod(data));
			}
		}
		#endregion Filter

		#region Sorting
		private System.Func<object, object, int> sortingMethod = null;
		public void SetSorting(System.Func<object, object, int> sortingMethod)
		{
			this.sortingMethod = sortingMethod;
			SortAll();
		}

		private void SortAll()
		{
			if (sortingMethod is null)
			{
				return;
			}

			var kvps = m_SpawnedDict.ToList();

			kvps.Sort((a, b) => sortingMethod(a.Value, b.Value));

			//assume all the gameobjects in the same parent is inside _dataToControl
			for (int i = 0; i < kvps.Count; ++i)
			{
				kvps[i].Key.transform.SetSiblingIndex(i);
			}
		}

		//default sortingMethod using object.ToString()
		private int DefaultSortingMethod(object a, object b)
		{
			if (a is null && b is null)
				return 0;

			if (a is null)
				return -1;

			if (b is null)
				return 1;

			return a.ToString().CompareTo(b.ToString());
		}
		#endregion Sorting

		private void Reset()
		{
			m_Pool= GetComponent<KxObjectPool>();
		}
		public void Awake()
		{
			if (m_SortByAlphabet && sortingMethod == null)
				sortingMethod = DefaultSortingMethod;
			pool.Event_Spawn += Pool_Event_Spawn;
			pool.Event_Despawn += Pool_Event_Despawn;
		}

		private void OnDestroy()
		{
			pool.Event_Spawn -= Pool_Event_Spawn;
			pool.Event_Despawn -= Pool_Event_Despawn;
		}

		private System.Action<object, GameObject> m_SpawnedCallback, m_DespawnCallback;
		public void SetDespawnCallback(System.Action<object, GameObject> callback) { m_DespawnCallback = callback; }
		private void Pool_Event_Despawn(GameObject token)
		{
			if (m_DespawnCallback == null)
				return;

			foreach ((var comp, var data) in m_SpawnedDict)
			{
				if (comp.gameObject != token)
					continue;
				m_DespawnCallback.Invoke(data, comp.gameObject);
				m_SpawnedDict.Remove(comp);
				return;
			}
		}

		public void SetSpawnedCallback(System.Action<object, GameObject> callback) { m_SpawnedCallback = callback; }
		private void Pool_Event_Spawn(GameObject token)
		{
		}

		public GameObject Spawn<T>(T data, int siblingIndex = -1)
			where T : class
		{

			foreach (var kvp in prefabDict)
			{
				if (kvp.Key.CanHandle(data))
				{
					var token = pool.Spawn(kvp.Value, m_SpawnLayer);
					if (siblingIndex >= 0)
					{
						token.transform.SetSiblingIndex(siblingIndex);
					}
					BindingBase handle = token.GetComponent<BindingBase>();
					Debug.Assert(handle != null);
					Debug.Assert(!m_SpawnedDict.ContainsKey(handle));
					m_SpawnedDict.Add(handle, data);
					handle.Assign(data, false);

					if (m_SpawnedCallback != null)
						m_SpawnedCallback.Invoke(data, token);
					return token;
				}
			}
			Debug.LogError($"Prefab on the list, fail to handle '{data.GetType()}', please check {nameof(prefabDict)} reference.", this);
			return null;
		}

		public void SpawnByDataList<T>(IEnumerable<T> data, bool despawnOld = true)
			where T : class
		{
			if (despawnOld)
			{
				DespawnAll();
			}
			foreach (var d in data)
			{
				Spawn(d);
			}
			SortAll();
			FilterAll();
		}

		public void DespawnAll()
		{
			var list = new List<GameObject>(pool.GetSpawnedObjects());
			int i = list.Count;
			while (i-- > 0)
			{
				pool.Despawn(list[i]);
			}
			list.Clear();
			m_SpawnedDict.Clear();
		}
	}
}