using Godot;
using System;
using TD.Models;
using TD.Misc;

namespace TD.Pages.TrackEditor;

public partial class TileButton : Button
{
	private TextureRect TextureRect;
	private Outline Outline;

	public Tile Tile { get; private set; }
	public bool Selected { get; private set; }
    
	
	public override void _Ready()
	{
		base._Ready();
		TextureRect = GetNode<TextureRect>("%TextureRect");
		Outline = GetNode<Outline>("%Outline");
		Outline.Hide();
	}


	public void SetTile(Tile tile)
	{
		Tile = tile;
		TextureRect.Texture = Game.GetTileTexture(tile);
	}

	public void SetSelected(bool selected)
	{
		Selected = selected;

		if (Selected)
			Outline.Show();
		else
			Outline.Hide();
	}


	private void OnMouseEntered()
	{
		if (Selected)
			return;
		
		Outline.Blink();
	}

	private void OnMouseExited()
	{
		if (Selected)
			return;
		
		Outline.Hide();
	}
}
