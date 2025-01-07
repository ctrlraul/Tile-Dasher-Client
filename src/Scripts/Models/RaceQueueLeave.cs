using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class RaceQueueLeave
{
	public string trackId;
	public string playerId;
}