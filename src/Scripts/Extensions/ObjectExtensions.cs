using Newtonsoft.Json;

namespace TD.Extensions;

public static class ObjectExtensions
{
	/// <summary>
	/// Clones an object using json serialization and deserialization
	/// </summary>
	public static T JsonClone<T>(this T obj)
	{
		return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
	}
}