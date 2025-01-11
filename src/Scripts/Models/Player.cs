using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class Player
{
	public static readonly Player Dummy = new()
	{
		name = "Developer",
		level = 10,
		email = "developer@game.com",
		id = "dev",
		createdAt = DateTime.Now,
		lastSeen = DateTime.Now,
		trackInfos = new List<TrackInfo>()
	};
    
	public string id;
	public DateTime createdAt;
	public DateTime lastSeen;
	public string email;
	public uint level;
	public string name;
	public string role;
	public List<TrackInfo> trackInfos;

	[JsonIgnore] public bool IsGuest => role == "guest";
}