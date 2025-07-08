using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
using Kit2.ObjectPool;
namespace Gaia
{
	using LayoutCell = UILayoutGroup.Cell;
    public abstract class SpawnCommandBase
    {
		public abstract int dataCount { get; }

		public void ClearAllSpawnedUI() { OnClearAllSpawnedUIs(); }
		public void DespawnCulledUIs(in RangeInt viewRange) { OnDespawnCulledUIs(viewRange); }
		public void SpawnUIs(in RangeInt viewRange) { OnSpawnUIs(viewRange); }
		public void AppendLayoutCells(List<LayoutCell> cells) { OnAppendLayoutCells(cells); }


		protected virtual void OnClearAllSpawnedUIs() { }
		protected virtual void OnDespawnCulledUIs(in RangeInt viewRange) { }
		protected virtual void OnSpawnUIs(in RangeInt viewRange) { }
		protected virtual void OnAppendLayoutCells(List<LayoutCell> cells) { }
	}

	public abstract class UISpawnCommand<DATA, UI, POOL> : SpawnCommandBase where UI : MonoBehaviour where POOL : KxObjectPool
	{
		[System.Serializable]
		public struct SpawnDesc
		{
			public POOL pool;
			public Transform root;
			public UI prefab;
		}

		public struct Result
		{
			public Dictionary<int, UI> spawnedUIs;

			public bool HasSpawned(int index)
			{
				return spawnedUIs.ContainsKey(index);
			}
		}

		/// <summary>
		/// require manually <see cref="Init(IReadOnlyList{DATA}, in SpawnDesc, Action{UI, DATA}, Action{UI})"/>
		/// </summary>
		public UISpawnCommand()
		{
			m_result.spawnedUIs = new Dictionary<int, UI>();
		}

		public UISpawnCommand(IReadOnlyList<DATA> data, POOL pool, Transform root, UI prefab, Action<UI, DATA> onSpawn, Action<UI> onDespawn) : base()
		{
			var desc = new SpawnDesc
			{
				pool = pool,
				root = root,
				prefab = prefab,
			};
			Init(data, desc, onSpawn, onDespawn);
		}

		public UISpawnCommand(IReadOnlyList<DATA> data, in SpawnDesc spawn, Action<UI, DATA> onSpawn, Action<UI> onDespawn) : base()
		{
			Init(data, spawn, onSpawn, onDespawn);
		}

		public override int dataCount => m_data != null ? m_data.Count : 0;
		public Result result => m_result;


		protected SpawnDesc m_spawn;
		protected IReadOnlyList<DATA> m_data;
		protected Result m_result;

		protected List<int> m_tempToDespawn = new List<int>();


		protected Action<UI, DATA> m_onSpawn = null;
		protected Action<UI> m_onDespawn = null;
		protected Dictionary<GameObject, UI> m_TokenCompMap = new Dictionary<GameObject, UI>(8);

		public void Init(IReadOnlyList<DATA> data, POOL pool, Transform root, UI prefab, Action<UI, DATA> onSpawn, Action<UI> onDespawn)
		{
			Reset(data);
			SetSpawnDesc(new SpawnDesc
			{
				pool = pool,
				root = root,
				prefab = prefab,
			});
			SetSpawnCallback(onSpawn);
			SetDespawnCallback(onDespawn);
		}

		public void Init(IReadOnlyList<DATA> data, in SpawnDesc spawn, Action<UI, DATA> onSpawn, Action<UI> onDespawn)
		{
			Reset(data);
			SetSpawnDesc(spawn);
			SetSpawnCallback(onSpawn);
			SetDespawnCallback(onDespawn);
		}

		internal void SetSpawnDesc(in SpawnDesc spawn) { m_spawn = spawn; }
		internal void Reset(IReadOnlyList<DATA> data) { m_data = data; }

		internal void SetSpawnCallback(Action<UI, DATA> onSpawn) { m_onSpawn = onSpawn; }
		internal void SetDespawnCallback(Action<UI> onDespawn) { m_onDespawn = onDespawn; }

		public bool CheckValidViewRange(in RangeInt viewRange)
		{
			if (viewRange.start < 0 || viewRange.end > m_data.Count)
			{
				Debug.LogError("AxUISpawnCommand : Invalid View Range");
				return false;
			}
			return true;
		}

		public virtual bool CheckValidSpawn(in SpawnDesc desc)
		{
			if (desc.pool == null || desc.root == null || desc.prefab == null)
			{
				Debug.LogError("AxUISpawnCommand.SpawnDesc : Invalid");
				return false;
			}
			return true;
		}


		UI SpawnUI(DATA data)
		{
			var ui = OnSpawnUI(data);
			try
			{
				m_onSpawn?.Invoke(ui, data);
			}
			catch (System.Exception e)
			{
				Debug.LogError($"fail to spawn [{data}] : [{e}]");
			}
			return ui;
		}

		void DespawnUI(UI ui)
		{
			try
			{
				m_onDespawn?.Invoke(ui);
			}
			catch (System.Exception e)
			{
				Debug.LogError($"fail to despawn [{ui}] : [{e}]");
			}
			OnDespawnUI(ui);
		}

		protected virtual UI OnSpawnUI(DATA data)
		{
			if (!CheckValidSpawn(m_spawn))
				return null;
			var spawned = m_spawn.pool.Spawn(m_spawn.prefab.gameObject, m_spawn.root);
			spawned.transform.SetAsLastSibling();
			return spawned.GetComponentCache<UI>(m_TokenCompMap);
		}

		protected virtual void OnDespawnUI(UI ui)
		{
			if (!CheckValidSpawn(m_spawn))
				return;
			m_spawn.pool.Despawn(ui.gameObject);
		}

		protected override void OnAppendLayoutCells(List<LayoutCell> cells)
		{
			if (cells == null)
				return;

			cells.Clear();
			cells.Capacity = result.spawnedUIs.Count;

			foreach (var uiInfo in result.spawnedUIs)
			{
				cells.Add(new LayoutCell()
				{
					index = uiInfo.Key,
					rect = uiInfo.Value.transform as RectTransform,
				});
			}
		}

		protected override void OnClearAllSpawnedUIs()
		{
			foreach (var ui in result.spawnedUIs)
			{
				if (ui.Value == null)
					continue;
				DespawnUI(ui.Value);
			}
			result.spawnedUIs.Clear();
		}

		protected override void OnDespawnCulledUIs(in RangeInt viewRange)
		{
			if (!CheckValidViewRange(viewRange))
				return;

			m_tempToDespawn.Clear();
			m_tempToDespawn.Capacity = result.spawnedUIs.Count;

			foreach (var ui in result.spawnedUIs)
			{
				if (ui.Value == null)
					continue;

				var idx = ui.Key;
				if (idx >= viewRange.start && idx < viewRange.end)
					continue;
				m_tempToDespawn.Add(ui.Key);
			}

			foreach (var despawnIdx in m_tempToDespawn)
			{
				DespawnUI(result.spawnedUIs[despawnIdx]);
				result.spawnedUIs.Remove(despawnIdx);
			}
		}

		protected override void OnSpawnUIs(in RangeInt viewRange)
		{
			if (!CheckValidViewRange(viewRange))
				return;

			for (int i = viewRange.start; i < viewRange.end; i++)
			{
				if (m_result.HasSpawned(i))
					continue;

				var spawned = SpawnUI(m_data[i]);
				if (spawned == null)
					continue;

				result.spawnedUIs[i] = spawned;
			}
		}
	}

	public class AxUISpawnCommand<DATA, UI> : UISpawnCommand<DATA, UI, KxObjectPool> where UI : MonoBehaviour
	{
	}

}