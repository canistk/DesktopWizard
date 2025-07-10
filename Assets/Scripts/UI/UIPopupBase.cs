using Kit2.ObjectPool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class UIPopupBase : MonoBehaviour, ISpawnToken
	{
		#region ISpawnToken
		private ISpawner m_Spawner;

		public void OnSpawn(ISpawner pool)
		{
			this.m_Spawner = pool;
		}

		public void SelfDespawn()
		{
			if (m_Spawner != null)
			{
				m_Spawner.Despawn(gameObject);
				m_Spawner = null;
			}
		}
		public void OnDespawn()
		{
		}
		#endregion ISpawnToken
	}
}