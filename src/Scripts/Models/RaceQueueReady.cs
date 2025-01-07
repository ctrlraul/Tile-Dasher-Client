using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class RaceQueueReady
{
	public string trackId;
	public string playerId;
}