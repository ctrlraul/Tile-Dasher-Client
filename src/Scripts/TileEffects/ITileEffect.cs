using Godot;
using TD.Models;

namespace TD.TileEffects;

public interface ITileEffect
{
	/// <summary>
	/// Causes this effect to make a tile's collision shape to be excluded from terrain merging
	/// </summary>
	public bool NonStatic { get; }
	
	public void Trigger(Tile tile, Vector2I coord, Character character);
}