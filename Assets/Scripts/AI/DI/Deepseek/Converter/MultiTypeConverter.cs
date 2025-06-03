using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Deepseek
{
	public class MultiTypeConverter : JsonConverter
	{
		public override bool CanConvert(System.Type objectType)
		{
			return objectType == typeof(object);
		}

		public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
		{
			JToken token = JToken.Load(reader);
			if (token.Type == JTokenType.Null)
			{
				return null;
			}
			else if (token.Type == JTokenType.String)
			{
				return token.ToObject<string>();
			}
			else if (token.Type == JTokenType.Array)
			{
				return token.ToObject<string[]>();
			}
			throw new JsonSerializationException("Unexpected token type: " + token.Type);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
			}
			else if (value is string)
			{
				writer.WriteValue((string)value);
			}
			else if (value is string[])
			{
				serializer.Serialize(writer, value);
			}
			else
			{
				throw new JsonSerializationException("Unexpected object type: " + value.GetType());
			}
		}
	}
}