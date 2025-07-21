using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Gaia
{
    public static class GxUtil
    {
        public static string ToJson(object? value)
        {
            return JsonConvert.SerializeObject(value);
		}

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}