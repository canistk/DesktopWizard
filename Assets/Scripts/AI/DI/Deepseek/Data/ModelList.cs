using Newtonsoft.Json;
namespace Deepseek
{
	public struct ModelList
	{
		[JsonProperty("object")]	public string obj;
		[JsonProperty("data")]		public ModelData[] data;
	}

	public struct ModelData
	{
		[JsonProperty("id")]		public string id;
		[JsonProperty("object")]	public string obj;
		[JsonProperty("owned_by")]	public string ownedBy;
	}
}