using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
namespace Deepseek.Functions
{
    public class FunctionCalling
    {
        private static CustomizedFunction[] _FunctionTypes = null;
		public static CustomizedFunction[] s_FunctionTypes
        {
            get
            {
                if (_FunctionTypes == null)
                {
					_FunctionTypes = System.Reflection.Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(CustomizedFunction)))
                        .Select(t => (CustomizedFunction)System.Activator.CreateInstance(t))
                        .ToArray();
				}
                return _FunctionTypes;
            }
        }

        private static Dictionary<string, CustomizedFunction> _FunctionDict = null;

		public static Dictionary<string, CustomizedFunction> s_FunctionDict
        {
            get
            {
				if (_FunctionDict == null)
                {
					_FunctionDict = new Dictionary<string, CustomizedFunction>(s_FunctionTypes.Length);
					foreach (var func in s_FunctionTypes)
                    {
                        _FunctionDict.Add(func.name, func);
                    }
				}
                return _FunctionDict;
			}
        }

        public static async Task<ToolMessage> TryInvoke(string tool_call_id, FunctionRequest functionRequest)
		{
			if (!s_FunctionDict.TryGetValue(functionRequest.name, out var function))
			{
				Debug.LogError($"[ERROR:Function not exist,\n{functionRequest}]");
				return null;
			}
			JObject args = null;
			try
			{
				args = string.IsNullOrEmpty(functionRequest.arguments)
					? new JObject()
					: JObject.Parse(functionRequest.arguments);
				var reply = await function.Execute(tool_call_id, args);
				return reply;
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[ERROR:Execute Fail.\n{functionRequest}\n{ex.Message}");
				return null;
			}
		}
	}
}