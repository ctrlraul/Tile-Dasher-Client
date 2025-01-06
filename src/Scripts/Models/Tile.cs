using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace TD.Models;

public class TileEffectConfig
{
	public string trigger;
	public string effect;
}

[JsonObject]
public class Tile
{
	public long id;
	public string name;
	public int atlasX;
	public int atlasY;
	public int matter;
	public bool safe;
	public float friction;
	public bool listed;
	public List<TileEffectConfig> effects;

	[JsonIgnore] public Vector2I AtlasCoords => new(atlasX, atlasY);
}