using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class Track
{
	public string id;
	public DateTime createdAt;
	public string name;
	public long plays;
	public Dictionary<long, Tile> customTiles;
	public Dictionary<long, List<int>> tileCoords;
}