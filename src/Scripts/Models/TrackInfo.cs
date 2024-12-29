using System;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class TrackInfo
{
	public string id;
	public DateTime createdAt;
	public string name;
	public long plays;
}