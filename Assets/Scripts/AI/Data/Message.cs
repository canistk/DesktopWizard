using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AI
{
    public abstract class MessageBase
	{
		[JsonProperty("role")] public string role;
		[JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)] public string content;
		public MessageBase(string role, string content)
		{
			this.role = role;
			this.content = content != null ? content : null;
		}
	}


	public class SystemMessage : MessageBase
	{
		public SystemMessage(string content) : base("system", content) { }
	}

	public class UserMessage : MessageBase
	{
		[JsonProperty("name")] public string name;
		public UserMessage(string name, string content) : base("user", content)
		{
			this.name = name;
		}

		public UserMessage(string content) : base("user", content)
		{
			this.name = string.Empty;
		}
	}

	public class AssistantMessage : MessageBase
	{
		[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)] public string name;
		public AssistantMessage(string content) : this(null, content) { }

		public AssistantMessage(string name, string content) : base("assistant", content)
		{
			this.name = name;
		}
	}

}