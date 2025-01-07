using System.Collections.Generic;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class RacesQueueEntry
{
	public string trackId;
	public Dictionary<string, PlayerProfile> players = new();
	public List<string> playersReady = [];
}