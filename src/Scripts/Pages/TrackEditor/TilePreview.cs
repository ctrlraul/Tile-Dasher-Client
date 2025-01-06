using Godot;
using System;
using TD.Models;

namespace TD.Pages.TrackEditor;

public partial class TilePreview : Sprite2D
{
	public Vector2I Coord { get; private set; }
	
	
	public override void _Process(double delta)
	{
		base._Process(delta);
		Update();
	}

	
	public void Update()
	{
		Vector2 mouse = Stage.Instance.GetLocalMousePosition();
		Coord = Game.WorldToGrid(mouse);
		Position = Coord * Game.Config.tileSize;
	}

	public void SetTile(Tile tile)
	{
		Texture = Game.GetTileTexture(tile);
	}
}
