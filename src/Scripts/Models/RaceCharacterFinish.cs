using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class RaceCharacterFinish
{
	public string playerId;
	public long time;
}