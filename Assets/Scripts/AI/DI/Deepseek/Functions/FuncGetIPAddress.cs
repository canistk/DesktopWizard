using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
namespace Deepseek.Functions
{
	public class FuncGetIPAddress : CustomizedFunction
	{
		public override FunctionTemplate Define()
		{
			return new FunctionTemplate(GetType().Name, "Get user computer's IP address.");
		}

		public override System.Threading.Tasks.Task<ToolMessage> Execute(string tool_call_id, JObject obj)
		{
			
			var addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
			var arr = new JArray();
			foreach (var address in addressList)
			{
				arr.Add(address.ToString());
			}

			JObject rst = new JObject
			{
				["IPAddresses"] = arr
			};

			var msg = new ToolMessage(tool_call_id, rst.ToString());
			return Task.FromResult(msg);
		}
	}
}