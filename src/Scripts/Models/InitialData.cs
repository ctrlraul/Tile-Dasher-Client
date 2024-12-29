using System.Collections.Generic;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class InitialData
{
	public List<Tile> tiles;
	public Player player;
}