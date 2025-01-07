using System;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class PlayerProfile
{
	public string id;
	public string name;
	public uint level;
	public DateTime lastSeen;
}