using Godot;
using TD.Models;

namespace TD.TileEffects;

public class TileEffectBreak : ITileEffect
{
	public bool NonStatic { get; private set; } = true;
	
	public void Trigger(Tile tile, Vector2I coord, Character character)
	{
		Stage.TileGrid.RemoveTile(coord);
	}
}