using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class RaceQueueEnter
{
	public string trackId;
	public PlayerProfile player;
}