using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Deepseek
{
    public static class Utils
	{
		private static JsonSerializerSettings m_JsonSetting = null;
		private static JsonSerializerSettings JsonSetting
		{
			get
			{
				if (m_JsonSetting == null)
				{
					m_JsonSetting = new JsonSerializerSettings()
					{
						NullValueHandling = NullValueHandling.Ignore,
						ReferenceLoopHandling = ReferenceLoopHandling.Error,
						// PreserveReferencesHandling = PreserveReferencesHandling.Objects,
						Converters = {
							new MessageConverter(),
							new MultiTypeConverter(),
						},
					};
				}
				return m_JsonSetting;
			}
		}
		public static string ToJson(object obj)
		{
			return JsonConvert.SerializeObject(obj, Formatting.None, JsonSetting);
		}

		public static T FromJson<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json, JsonSetting);
		}
	}
}