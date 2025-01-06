using Godot;
using System;
using TD.Models;

namespace TD.Particles;

public partial class CrumbleParticles : GpuParticles2D
{
	public void Init(Vector2I coord)
	{
		Tile tile = Stage.TileGrid.GetTile(coord);
		Position = coord * Game.Config.tileSize;
		Modulate = TilesManager.GetColor(tile.id);
		Emitting = true;
	}
    
	private void OnFinished()
	{
		QueueFree();
	}
}
