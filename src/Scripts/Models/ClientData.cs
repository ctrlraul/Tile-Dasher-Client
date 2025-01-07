using System.Collections.Generic;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class ClientData
{
	public List<Tile> tiles;
	public Player player;
	public List<TrackInfo> latestTracks;
	public Dictionary<string, RacesQueueEntry> racesQueue;
}