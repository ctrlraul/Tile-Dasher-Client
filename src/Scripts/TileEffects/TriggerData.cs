using Godot;
using TD.Models;
using TD.Entities;

namespace TD.TileEffects;

public class TriggerData
{
	public Tile tile;
	public Vector2I coord;
	public Character character;
	public string trigger;
}