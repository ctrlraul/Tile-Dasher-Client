using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TD.Pages.Hud;

public partial class Minimap : Control
{
	private const int Margin = 0;
	
	private Control MapArea;
	private Control MapScaler;
	private TextureRect TextureRect;
	public Control Characters;


	private Vector2I MapSize;
	private Vector2I Center;

	
	public override void _Ready()
	{
		base._Ready();
		MapArea = GetNode<Control>("%MapArea");
		MapScaler = GetNode<Control>("%MapScaler");
		TextureRect = GetNode<TextureRect>("%TextureRect");
		Characters = GetNode<Control>("%Characters");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		List<Character> characters = Stage.GetCharacters().ToList();

		for (int i = 0; i < characters.Count; i++)
		{
			Character character = characters[i];
			Control indicator = Characters.GetChild<Control>(i);
			indicator.Position = (-Center + character.Position / Game.Config.tileSize) * (TextureRect.Texture.GetSize() / MapSize);
		}
	}


	public void Generate(bool[,] grid, Vector2I center)
	{
		MapSize = new Vector2I(
			grid.GetLength(1),
			grid.GetLength(0)
		);

		Center = center;

		Image image = Image.CreateEmpty(
			Margin * 2 + MapSize.X, 
			Margin * 2 + MapSize.Y, 
			false,
			Image.Format.Rgba8
		);

		for (int y = 0; y < MapSize.Y; y++)
		{
			for (int x = 0; x < MapSize.X; x++)
			{
				if (grid[y, x])
					image.SetPixel(Margin + x, Margin + y, Colors.Black);
			}
		}

		TextureRect.Texture = ImageTexture.CreateFromImage(image);

		// Has to run after the first draw
		CallDeferred(MethodName.UpdateTextureScale);
	}

	private void UpdateTextureScale()
	{
		Vector2 textureSize = TextureRect.Texture.GetSize();
		
		float scale = textureSize.X / MapArea.Size.X > textureSize.Y / MapArea.Size.Y
			? MapArea.Size.X / textureSize.X
			: MapArea.Size.Y / textureSize.Y;
		
		MapScaler.Scale = Vector2.One * scale;
	}
}
