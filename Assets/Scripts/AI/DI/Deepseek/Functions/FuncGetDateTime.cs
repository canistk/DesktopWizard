using Newtonsoft.Json.Linq;
//using Newtonsoft.Json.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Deepseek.Functions
{
	public class FuncGetDateTime : CustomizedFunction
	{
		public override FunctionTemplate Define()
		{
			return new FunctionTemplate(GetType().Name, "Get the current system time.");
		}

		public override Task<ToolMessage> Execute(string tool_call_id, JObject obj)
		{
			var current = DateTime.UtcNow;
			var timeStr = current.ToString("yyyy-MM-dd HH:mm:ss");
			Debug.Log($"{GetType()}: {timeStr}");
			var rst = new JObject
			{
				["UtcNow"] = timeStr
			};
			var msg = new ToolMessage(tool_call_id, rst.ToString());
			return Task.FromResult(msg);
		}
	}
}