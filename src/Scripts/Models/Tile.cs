using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class Tile
{
	public long id;
	public string name;
	public int atlasX;
	public int atlasY;
	public int matter;
	public bool safe;
	public Dictionary<string, List<string>> effects;

	[JsonIgnore] public Vector2I AtlasCoords => new(atlasX, atlasY);
}