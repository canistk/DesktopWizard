using System.Collections.Generic;
using Newtonsoft.Json;
namespace DesktopWizard
{
	public class MTAction : Dictionary<string, object>, System.IDisposable
	{
		public MTAction(string value)
		{
			this.Add("action", value);
		}
		public override string ToString() => ToJson();
		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}

		public void Dispose()
		{
			this.Clear();
		}
	}
}