using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft;
using Newtonsoft.Json.Linq;
namespace Deepseek
{
	public class MessageConverter : JsonConverter
	{
		public override bool CanConvert(System.Type objectType)
		{
			return
				typeof(Message).IsAssignableFrom(objectType) ||
				typeof(Message[]).IsAssignableFrom(objectType) ||
				typeof(List<Message>).IsAssignableFrom(objectType);
		}

		public override bool CanWrite => true;
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is IList<Message> messages)
			{
				var jsonArray = new JArray();
				foreach (var message in messages)
				{
					var jsonObject = JObject.FromObject(message, serializer);
					jsonArray.Add(jsonObject);
				}
				jsonArray.WriteTo(writer);
			}
			else if (value == null)
			{
				writer.WriteNull();
			}
			else
			{
				var jsonObject = JObject.FromObject(value);
				jsonObject.WriteTo(writer);
			}
		}

		public override bool CanRead => true;
		public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (objectType == typeof(Message))
			{
				var jsonObject = JObject.Load(reader);
				var role = jsonObject["role"]?.ToString();
				var message = CreateMessage(jsonObject, role);
				serializer.Populate(jsonObject.CreateReader(), message);
				return message;
			}
			else if (objectType == typeof(List<Message>) || objectType == typeof(Message[]))
			{
				JArray jsonArray = JArray.Load(reader);
				var messages = new List<Message>();
				foreach (var item in jsonArray)
				{
					var jsonObject = (JObject)item;
					var role = jsonObject["role"]?.ToString();
					var message = CreateMessage(jsonObject, role);
					serializer.Populate(jsonObject.CreateReader(), message);
					messages.Add(message);
				}

				if (objectType == typeof(Message[]))
					return messages.ToArray();
				return messages;
			}
			else
			{
				var token = JToken.Load(reader);
				if (token.Type == JTokenType.Null)
				{
					return null;
				}
				throw new System.NotImplementedException();
			}
		}

		private static Message CreateMessage(JObject jsonObject, string role)
		{
			if (jsonObject == null)
				return null;

			var content = jsonObject["content"]?.Value<string>() ?? null;

			switch (role)
			{
				case "system":
				{
					return new SystemMessage(content);
				}
				case "user":
				{
					var name = jsonObject["name"]?.Value<string>() ?? string.Empty;
					return new UserMessage(name, content);
				}
				case "assistant":
				{
					var name = jsonObject["name"]?.Value<string>() ?? string.Empty;
					var prefix = jsonObject["prefix"]?.Value<bool>() ?? false;
					var reasoning = jsonObject["reasoning_content"]?.Value<string>() ?? null;
					return  new AssistantMessage(name, content, prefix, reasoning);
				}
				case "tool":
				{
						var tid = jsonObject["tool_call_id"]?.Value<string>() ?? string.Empty;
						return new ToolMessage(tid, content);
				}
				default:
					throw new System.NotSupportedException($"Unknown role: {role}");
			}
		}
	}
}