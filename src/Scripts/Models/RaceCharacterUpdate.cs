using Godot;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class RaceCharacterUpdate
{
	public string id;
	
	public float x;
	public float y;
	public float vx;
	public float vy;

	public int ih;
	public int iv;

	[JsonIgnore] public Vector2 Position => new(x, y);
	[JsonIgnore] public Vector2 Velocity => new(vx, vy);
	[JsonIgnore] public int HorizontalInput => ih;
	[JsonIgnore] public int VerticalInput => iv;
}