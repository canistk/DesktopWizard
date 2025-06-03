using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
namespace Deepseek
{
	public abstract class Message
	{
		/// <summary>
		/// Possible values [system, user, assistant, tool]
		/// </summary>
		[JsonProperty("role")]		public string role;
		[JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]	public string content;
		public Message(string role, string content)
		{
			this.role = role;
			this.content = content != null ? content : null;
		}
	}
	
	public class SystemMessage : Message
	{
		public SystemMessage(string content) : base("system", content) { }
	}
	
	public class UserMessage : Message
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

	public class AssistantMessage : Message
	{
		[JsonProperty("prefix", NullValueHandling = NullValueHandling.Ignore)]				public bool? prefix;
		[JsonProperty("reasoning_content", NullValueHandling = NullValueHandling.Ignore)]	public string reasoningContent;
		[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]				public string name;
		[JsonProperty("tool_calls", NullValueHandling = NullValueHandling.Ignore)]			public List<ToolCall> toolCalls;
		public AssistantMessage(string content) : this(null, content, false, null) { }

		public AssistantMessage(string name, string content) : this(name, content, false, null) { }
		public AssistantMessage(string name, string content, bool prefix, string reasoningContent) : base("assistant", content)
		{
			this.name = name;
			this.prefix = prefix ? prefix : null; // set null instead of set false.
			this.reasoningContent = reasoningContent;
		}
	}
	
	public class ToolMessage : Message
	{
		[JsonProperty("tool_call_id")] public string tool_call_id;
		public ToolMessage(string tool_call_id, string content) : base("tool", content)
		{
			this.tool_call_id = tool_call_id;
		}
	}
}