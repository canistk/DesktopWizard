using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public abstract class GxUISettingBase : MonoBehaviour
    {
		public abstract int GetIndex();

		protected abstract void SetIndex(int index);

		public abstract string[] GetOptionNames();

		public abstract GxSettingBase GetSource();

		protected virtual void OnEnable()
		{
			GetSource().EVENT_Updated += OnValueUpdated;
		}
		protected virtual void OnDisable()
		{
			GetSource().EVENT_Updated -= OnValueUpdated;
		}
		protected virtual void OnValueUpdated()
		{
			// read value from source
			var val = GetSource().GetRawValue();
		}
	}

	public abstract class GxUISetting<T> : GxUISettingBase
	{
		private KeyValuePair<T, string>[] m_Cache;
		protected KeyValuePair<T, string>[] cache
		{
			get
			{
				if (m_Cache == null)
				{
					var values = GetOptionValues();
					var names = GetOptionNames();
					if (values == null || names == null)
						throw new System.Exception($"Option's value/name cannot be null.");
					if (values.Length != names.Length)
						throw new System.Exception($"Option's length not match, names={names.Length}, values={values.Length}");
					m_Cache = new KeyValuePair<T, string>[values.Length];
					for (int i = 0; i < values.Length; ++i)
					{
						m_Cache[i] = new KeyValuePair<T, string>(values[i], names[i]);
					}
				}
				return m_Cache;
			}
		}

		protected abstract T[] GetOptionValues();

		public abstract GxSetting<T> GetSourceWithType();
		public sealed override GxSettingBase GetSource() => GetSourceWithType();

		protected override void OnEnable()
		{
			// base.OnEnable();
			GetSourceWithType().EVENT_Updated += OnValueUpdated;
		}

		protected override void OnDisable()
		{
			// base.OnDisable();
			GetSourceWithType().EVENT_Updated -= OnValueUpdated;
		}

		protected virtual void SetValue(T value)
		{
			if (!TryGetIndex(value, out var index))
				throw new System.Exception($"Invalid value={value}, it's not within options.");
			SetIndex(index);
		}

		public T GetValue() => cache[GetIndex()].Key;

		protected override void SetIndex(int index)
		{
			var value = GetOptionValues()[index];
			GetSourceWithType().Save(value);
		}

		public override int GetIndex()
		{
			if (!TryGetIndex(GetSourceWithType().value, out var index))
				return 0;
			return index;
		}

		private bool TryGetIndex(T value, out int index)
		{
			for (int i = 0; i < cache.Length; ++i)
			{
				if (!cache[i].Key.Equals(value))
					continue;
				index = i;
				return true;
			}
			index = -1;
			return false;
		}
		protected sealed override void OnValueUpdated()
		{
			// base.OnValueUpdated();
			// read value from source
			var value = GetSourceWithType().value;
			if (!TryGetIndex(value, out var index))
			{
				// SetIndex(0);
				throw new System.Exception($"Invalid cache data {value}");
			}

			var name = cache[index].Value;
			OnSourceUpdated(index, name, value);
		}

		protected abstract void OnSourceUpdated(int index, string name, T value);
	}

}