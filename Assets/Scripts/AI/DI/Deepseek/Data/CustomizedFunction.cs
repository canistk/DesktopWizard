using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Deepseek
{
    public abstract class CustomizedFunction
    {
        private KeyValuePair<bool, FunctionTemplate> m_Function = default;
        public FunctionTemplate function
        {
            get
            {
                if (!m_Function.Key)
                {
                    m_Function = new KeyValuePair<bool, FunctionTemplate>(true, Define());
                }
                return m_Function.Value;
            }
        }
        public Tool GetFunctionCalling() => new Tool(function);

        public string name => function.name;
        public string description => function.description;
        public abstract FunctionTemplate Define();

        public abstract System.Threading.Tasks.Task<ToolMessage> Execute(string tool_call_id, JObject obj);
    }
}