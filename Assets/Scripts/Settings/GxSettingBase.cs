using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public abstract class GxSettingBase
    {
		public abstract void Save();
		public abstract void ResetPreview();

		public abstract bool IsDirty { get; }

		public event System.Action EVENT_Updated;
		protected void _TriggerUpdate()
		{
			EVENT_Updated?.Invoke();
		}

		public abstract object GetRawValue();
	}

	public class GxSetting<T> : GxSettingBase
	{
		private readonly string key;
		private T m_Value;
		public T value
		{
			get
			{
				if (m_Preview.Key)
					return m_Preview.Value;
				return m_Value;
			}
		}

		private KeyValuePair<bool, T> m_Preview = default;
		public override bool IsDirty => m_Preview.Key;

		public override object GetRawValue()
		{
			return value;
		}

		private System.Action<T> m_ApplyCallback;
		public event System.Action<T> EVENT_ValueChanged;
		public GxSetting(string key, T _default, System.Action<T> applyCallback = null)
		{
			this.key = key;
			this.m_Value = Load(_default);
			this.m_ApplyCallback = applyCallback;

			// - Mac OSX compiler vs Window compiler are different
			// -Mac will run the class constructor, and then assign address to "m_Instance" value
			// - Win will alloc memory to "m_Instance", before constructor completed.
			// InternalApply(value);
		}

		private void InternalApply(T value)
		{
			m_ApplyCallback?.Invoke(value);
			EVENT_ValueChanged?.Invoke(value);
			_TriggerUpdate();
		}

		private void InternalSave(T value)
		{
			if (EqualityComparer<T>.Default.Equals(m_Value, value))
				return;

			this.m_Value = value;
			var tt = typeof(T);
			if (tt == typeof(int) ||
				(tt.IsEnum && tt.GetEnumUnderlyingType() == typeof(int)))
			{
				PlayerPrefs.SetInt(key, Convert.ToInt32(value));
			}
			else if (tt == typeof(uint) ||
				(tt.IsEnum && tt.GetEnumUnderlyingType() == typeof(uint)))
			{
				var u = Convert.ToUInt32(value);
				PlayerPrefs.SetInt(key, Convert.ToInt32(u));
			}
			else if (tt == typeof(bool))
			{
				var val = Convert.ToBoolean(value) ? 1 : 0;
				PlayerPrefs.SetInt(key, val);
			}
			else if (tt == typeof(string))
			{
				PlayerPrefs.SetString(key, value.ToString());
			}
			else
			{
				// otherwise, struct, enum, try convert into JSON.
				try
				{
					var json = JsonUtility.ToJson(value);
					PlayerPrefs.SetString(key, json);
				}
				catch (System.Exception ex)
				{
					Debug.LogError(ex);
				}
			}
			InternalApply(value);
		}

		public T Load(T _default)
		{

			var tt = typeof(T);
			if (tt == typeof(int) ||
				(tt.IsEnum && tt.GetEnumUnderlyingType() == typeof(int)))
			{
				var num = PlayerPrefs.GetInt(key, Convert.ToInt32(_default));
				return (T)(object)num;
			}
			else if (tt == typeof(uint) ||
				(tt.IsEnum && tt.GetEnumUnderlyingType() == typeof(uint)))
			{
				var u = Convert.ToUInt32(_default);
				var n = Convert.ToInt32(u);
				var num = PlayerPrefs.GetInt(key, n);
				return (T)(object)num;
			}
			else if (tt == typeof(bool))
			{
				var val = Convert.ToBoolean(_default);
				var def = val ? 1 : 0;
				var num = PlayerPrefs.GetInt(key, def);
				var rst = num == 1 ? true : false;
				return (T)(object)rst;
			}
			else if (tt == typeof(string))
			{
				var str = PlayerPrefs.GetString(key, _default.ToString());
				return (T)(object)str;
			}
			else
			{
				var defJson = JsonUtility.ToJson(_default);
				var json = PlayerPrefs.GetString(key, defJson);
				return JsonUtility.FromJson<T>(json);
			}


			throw new System.NotImplementedException();
		}
		public void Preview(T value)
		{
			//Debug.Log($"Preview Setting : {this.m_Value} -> {value}");
			m_Preview = new KeyValuePair<bool, T>(true, value);
			InternalApply(m_Preview.Value);
		}
		public override void ResetPreview()
		{
			if (!m_Preview.Key)
				return;
			m_Preview = default;
			InternalApply(value);
		}
		public void Apply()
		{
			if (!m_Preview.Key)
			{
				Debug.Log($"Setting : {value}");
				return;
			}
			Debug.Log($"Setting : {value} -> {m_Preview.Value}");
			InternalSave(m_Preview.Value);
			m_Preview = default;
			InternalApply(value);
		}
		public override void Save() => Apply();
		public void Save(T value)
		{
			Preview(value);
			Apply();
		}
	}
}