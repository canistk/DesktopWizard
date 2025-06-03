using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using Debug = UnityEngine.Debug;
using System;
using System.Text.RegularExpressions;
namespace Deepseek
{
	public static class eDeepseekModel
	{
		/// <summary>General chat</summary>
		public const string chat = "deepseek-chat";

		/// <summary>Deep think chat</summary>
		public const string reasoner = "deepseek-reasoner";
	}

	/// <summary>
	/// <see cref="https://api-docs.deepseek.com/zh-cn/quick_start/error_codes"/>
	/// </summary>
	public enum eErrorCode : long
	{
		Invalid_Format = 400, // Invalid request body format.
		Authentication_Fails = 401, // Authentication fails due to the wrong API key.
		Insufficient_Balance = 402, // Please check your account's balance, and go to the Top up page to add funds.
		Invalid_Parameters = 422, // Your request contains invalid parameters.
		Rate_Limit_Reached = 429, // You are sending requests too quickly.
		Server_Error = 500, // Please retry your request after a brief wait and contact us if the issue persists.
		Server_Overloaded = 503, // Please retry your request after a brief wait, The server is overloaded due to high traffic.
	}

	/// <summary>
	///	to convert chat request into JSON object
	/// <see cref="https://api-docs.deepseek.com/api/create-chat-completion"/>
	/// deepseek will response <see cref="ChatCompletion"/>
	/// </summary>
	public class Chat
	{
		/// <summary>Create you chat database.</summary>
		/// <param name="model">Could be 
		/// <see cref="eDeepseekModel.chat"/>
		/// <seealso cref="eDeepseekModel.reasoner"/></param>
		/// <param name="steam">Streaming will return part of message before generation completed.</param>
		public Chat(string model = "", bool steam = false)
		{
			this.model = model;

			// only assign on demend (avoid json size grow)
			this.stream = steam ? true : null;
		}

		public enum ErrorHandle : int
		{
			Ignore = 0,
			Error,
			Exception,
		}
		public bool IsValid(ErrorHandle h = ErrorHandle.Ignore)
		{
			//if (model == null || !(model == "deepseek-chat" || model == "deepseek-reasoner"))
			//{
			//	var msg = $"invalid Chat model : {model}";
			//	if (h == ErrorHandle.Error)
			//		Debug.LogError(msg);
			//	else if (h == ErrorHandle.Exception)
			//		throw new System.Exception(msg);
			//	return false;
			//}

			if (messages == null || messages.Count == 0)
			{
				var msg = $"No message to send";
				if (h == ErrorHandle.Error)
					Debug.LogError(msg);
				else if (h == ErrorHandle.Exception)
					throw new System.Exception(msg);
				return false;
			}

			if (frequencyPenalty.HasValue && (frequencyPenalty.Value < -2f || frequencyPenalty.Value > 2f))
			{
				var msg = $"Frequency Penalty ({frequencyPenalty.Value}) only accept -2 ~ 2 numbers, default = 0f";
				if (h == ErrorHandle.Error)
					Debug.LogError(msg);
				else if (h == ErrorHandle.Exception)
					throw new System.Exception(msg);
				return false;
			}

            if (maxTokens.HasValue && (maxTokens.Value < 1 || maxTokens.Value > 8192))
            {
				var msg = $"max tokens ({maxTokens.Value}) only accept 1 ~ 8192";
				if (h == ErrorHandle.Error)
					Debug.LogError(msg);
				else if (h == ErrorHandle.Exception)
					throw new System.Exception(msg);
				return false;
            }

			if (presencePenalty.HasValue && (presencePenalty.Value < -2f || presencePenalty.Value > 2f))
			{
				var msg = $"Presence Penalty ({presencePenalty.Value}) only accept -2 ~ 2 numbers, default 0f";
				if (h == ErrorHandle.Error)
					Debug.LogError(msg);
				else if (h == ErrorHandle.Exception)
					throw new System.Exception(msg);
				return false;
			}

            return true;
		}

		[JsonProperty("model")] public string model;

		[JsonConverter(typeof(MessageConverter))]
		[JsonProperty("messages")] public List<Message> messages = new List<Message>();
		public void AddSystemPrompt(string content) => messages.Add(new SystemMessage(content));
		public void RemoveSystemPrompt() => messages.RemoveAll(m => m.role == "system");

		public void AddUserMessage(string content) => messages.Add(new UserMessage(content));
		public void AddUserMessage(string name, string content) => messages.Add(new UserMessage(name, content));
		public void RemoveUserMessage() => messages.RemoveAll(m => m.role == "user");
		public void RemoveUserMessage(string name) => messages.RemoveAll(m => m.role == "user" && (m as UserMessage).name == name);

		public void AddAssistantMessage(string name, string content) => messages.Add(new AssistantMessage(name, content, false, null));
		public void AddAssistantMessage(string name, string content, bool prefix, string reasoningContent) => messages.Add(new AssistantMessage(name, content, prefix, reasoningContent));
		public void RemoveAssistantMessage() => messages.RemoveAll(m => m.role == "assistant");
		public void RemoveAssistantMessage(string name) => messages.RemoveAll(m => m.role == "assistant" && (m as AssistantMessage).name == name);

		public void AddToolMessage(string tool_call_id, string content) => messages.Add(new ToolMessage(tool_call_id, content));
		public void RemoveToolMessage() => messages.RemoveAll(m => m.role == "tool");
		public void RemoveAllMessages() => messages.Clear();

		/// <summary>
		/// Possible values: >= -2 and <= 2
		/// Default value: 0
		/// Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
		/// </summary>
		[JsonProperty("frequency_penalty", NullValueHandling = NullValueHandling.Ignore)] public float? frequencyPenalty = null;    // -2 ~ 2, default 0

		/// <summary>
		/// Possible values: > 1
		/// Integer between 1 and 8192. The maximum number of tokens that can be generated in the chat completion.
		/// The total length of input tokens and generated tokens is limited by the model's context length.
		/// If max_tokens is not specified, the default value 4096 is used.
		/// </summary>
		[JsonProperty("max_tokens", NullValueHandling = NullValueHandling.Ignore)] public int? maxTokens = null;
		public void SetMaxTokens(int t)
		{
			if (t < 1 || t > 8192)
				throw new System.Exception($"max tokens ({t}) only accept 1 ~ 8192");
			maxTokens = Math.Clamp(t, 1, 8192);
		}
		public void ResetMaxTokens() => maxTokens = null;

		/// <summary>
		/// Possible values: >= -2 and <= 2
		/// Default value: 0
		/// Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
		/// </summary>
		[JsonProperty("presence_penalty", NullValueHandling = NullValueHandling.Ignore)] public float? presencePenalty = null;
		public void SetPresencePenalty(float t)
		{
			if (t < -2f || t > 2f)
				throw new System.Exception($"Presence Penalty ({t}) only accept -2 ~ 2 numbers, default 0f");
			presencePenalty = Math.Clamp(t, -2f, 2f);
		}
		public void ResetPresencePenalty() => presencePenalty = null;

		/// <summary>
		/// Must be one of "text" or "json_object".
		/// </summary>
		[JsonProperty("response_format", NullValueHandling = NullValueHandling.Ignore)] public ResponseFormat? responseFormat = null;
		public void SetResponseFormat(bool isText) => responseFormat = isText ? ResponseFormat.TEXT : ResponseFormat.JSON_OBJECT;
		public void ResetResponseFormat() => responseFormat = null;

		/// <summary>
		/// null, string, or string[], to add context at the end of message
		/// e.g. "```" to ensure closed the code block.
		/// 
		/// e.g.2 work with <see cref="AssistantMessage"/> . Prefix = true, reasoning_content = "```" to ensure AI return code block message.
		/// </summary>
		[JsonConverter(typeof(MultiTypeConverter))]
		[JsonProperty("stop", NullValueHandling = NullValueHandling.Ignore)] public object stop = null;
		public void SetStopBlock(string stopStr) => stop = stopStr;
		public void ResetStopBlock() => stop = null;

		/// <summary>
		/// If set, partial message deltas will be sent.
		/// Tokens will be sent as data-only server-sent events (SSE) as they become available,
		/// with the stream terminated by a data: [DONE] message.
		/// </summary>
		[JsonProperty("stream", NullValueHandling = NullValueHandling.Ignore)] public bool? stream = null;

		/// <summary>
		/// Options for streaming response. Only set this when you set stream: true.
		/// include_usage : boolean
		/// If set, an additional chunk will be streamed before the data: [DONE] message.The usage field on this chunk shows the token usage statistics for the entire request, and the choices field will always be an empty array.All other chunks will also include a usage field, but with a null value.
		/// </summary>
		[JsonProperty("stream_options", NullValueHandling = NullValueHandling.Ignore)] public object streamOptions = null;
		public void SetStream(bool isStream) => this.stream = isStream ? true : null;
		public void ResetStream() => stream = null;

		/// <summary>
		/// Possible values: <= 2
		/// Default value: 1
		/// What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
		/// We generally recommend altering this or top_p but not both.
		/// </summary>
		[JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)] public float? temperature = null;
		public void SetTemperature(float t)
		{
			if (t < 0f || t > 2f)
				throw new System.Exception($"Temperature ({t}) only accept 0 ~ 2 numbers, default 1f");
			temperature = Math.Clamp(t, 0f, 2f);
		}
		public void ResetTemperature() => temperature = null;

		/// <summary>
		/// Possible values: <= 1
		/// Default value: 1
		/// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass.So 0.1 means only the tokens comprising the top 10% probability mass are considered.
		/// We generally recommend altering this or temperature but not both.
		/// </summary>
		[JsonProperty("top_p", NullValueHandling = NullValueHandling.Ignore)] public float? topP = null;
		public void SetTopP(float t)
		{
			if (t < 0f || t > 1f)
				throw new System.Exception($"Top P ({t}) only accept 0 ~ 1 numbers, default 1f");
			topP = Math.Clamp(t, 0f, 1f);
		}
		public void ResetTopP() => topP = null;

		/// <summary>
		/// A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
		/// Possible values: [function]
		/// The type of the tool.Currently, only function is supported.
		/// </summary>
		[JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)] public List<Tool> tools = null;
		public void ClearTools() => tools = null;
		public void AddTools(string name, string description, object parameters = null)
		{
			var func = new FunctionTemplate(name, description, parameters);
			AddTool(new Tool(func));
		}
		public void AddTool(CustomizedFunction value) => AddTool(value.GetFunctionCalling());

		public void AddTool(params CustomizedFunction[] tools)
		{
			for (int i = 0;  i < tools.Length; ++i)
				AddTool(tools[i]);
		}

		public void AddTool(Tool tool)
		{
			if (tools == null)
				tools = new List<Tool>();
			tools.Add(tool);
		}


		/// <summary>
		/// Controls which (if any) tool is called by the model.
		/// none means the model will not call any tool and instead generates a message.
		/// auto means the model can pick between generating a message or calling one or more tools.
		/// required means the model must call one or more tools.
		/// Specifying a particular tool via { "type": "function", "function": { "name": "my_function"} }
		/// forces the model to call that tool.
		/// none is the default when no tools are present.auto is the default if tools are present
		/// 
		/// 1) ChatCompletionToolChoice
		/// Possible values: [none, auto, required]
		/// 
		/// 2) ChatCompletionNamedToolChoice
		/// { "type": "function", "function": { "name": "my_function"} };
		/// </summary>
		[JsonProperty("tool_choice", NullValueHandling = NullValueHandling.Ignore)] public object toolChoice = null;

		/// <summary>
		/// allow to use 3rd party tools, e.g. web-clawer
		/// the integration of deepseek-coder into the open-source large model frontend LobeChat. <see cref="https://github.com/lobehub/lobe-chat"/>
		/// In this example, we enabled the "Website Crawler" plugin to perform website crawling and summarization.
		/// <seealso cref="https://api-docs.deepseek.com/news/news0725"/>
		/// </summary>
		[JsonProperty("logprobs", NullValueHandling = NullValueHandling.Ignore)] public bool? logprobs = null;

		/// <summary>Possible values: <= 20
		/// An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability.logprobs must be set to true if this parameter is used.
		/// </summary>
		[JsonProperty("top_logprobs", NullValueHandling = NullValueHandling.Ignore)] public int? topLogprobs = null;

		// Only n = 1 supported, return different choices, Non-API function
		// [JsonProperty("n", NullValueHandling = NullValueHandling.Ignore)] public int n = 1;
	}

	public struct ResponseFormat : IEqualityComparer<ResponseFormat>
	{
		public static readonly ResponseFormat TEXT			= new ResponseFormat() { type = "text" };
		public static readonly ResponseFormat JSON_OBJECT	= new ResponseFormat() { type = "json_object" };
		public string type;

		public bool Equals(ResponseFormat x, ResponseFormat y) => x.type == y.type;
		public int GetHashCode(ResponseFormat obj) => obj.type.GetHashCode();
	}

	public struct ChatCompletionError
	{
		[JsonProperty("error")] public ChatCompletionErrorDetail error;
	}

	public struct ChatCompletionErrorDetail
	{
		[JsonProperty("message")] public string message;
		[JsonProperty("type")] public string type;
		[JsonProperty("param")] public string param;
		[JsonProperty("code")] public string code;
	}

	/*
	tools = [
    {
        "type": "function",
        "function": {
            "name": "get_weather",
            "description": "Get weather of an location, the user should supply a location first",
            "parameters": {
                "type": "object",
                "properties": {
                    "location": {
                        "type": "string",
                        "description": "The city and state, e.g. San Francisco, CA",
                    }
                },
                "required": ["location"]
            },
        }
    },
	]
	 */
	public struct Tool
	{
		/// <summary>The type of the tool. Currently, only function is supported.</summary>
		[JsonProperty("type")]	public string type; // Ver R1, only support "function"
		[JsonProperty("function")] public FunctionTemplate function;
		public Tool(FunctionTemplate func)
		{
			this.type = "function";
			this.function = func;
		}
	}

	public struct FunctionTemplate
	{
		public FunctionTemplate(string name, string description, object parameters = null)
		{
			var pattern = new Regex(@"^[a-zA-Z0-9_-]{1,64}$");
			if (!pattern.IsMatch(name))
				throw new Exception($"[name] Invalid format\nMust be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.");
			this.name = name;
			this.description = description;
			this.parameters = parameters;
		}
		
		/// <summary>The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.</summary>
		[JsonProperty("name")] public string name;

		/// <summary>A description of what the function does, used by the model to choose when and how to call the function.</summary>
		[JsonProperty("description")] public string description;

		/// <summary>The parameters the functions accepts, 
		/// described as a JSON Schema object. <see cref="https://json-schema.org/understanding-json-schema"/>
		/// See the Function Calling Guide for examples, <see cref="https://api-docs.deepseek.com/guides/function_calling"/>
		/// and the JSON Schema reference for documentation about the format.
		/// Omitting parameters defines a function with an empty parameter list.</summary>
		[JsonProperty("parameters")] public object parameters;
	}

}