using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TD.Entities;

namespace TD.Pages.Hud;

public partial class Minimap : Control
{
	private const int Margin = 3;
	private static readonly Color PlayerColor = new(1, 1, 0);
	private static readonly Color OpponentColor = new(0, 0.5f, 1);

	private SubViewport SubViewport;
	private Sprite2D Sprite;
	private Camera2D Camera;
	private Node2D CharactersContainer;


	private Vector2I MapSize;
	private Vector2 Center;
	private Vector2 Offset;
	private bool OddWidth;
	private bool OddHeight;

	
	public override void _Ready()
	{
		base._Ready();
		SubViewport = GetNode<SubViewport>("%SubViewport");
		Sprite = GetNode<Sprite2D>("%Sprite2D");
		Camera = GetNode<Camera2D>("%Camera2D");
		CharactersContainer = GetNode<Node2D>("%CharactersContainer");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	
		List<Character> characters = Stage.GetCharacters().ToList();
		List<Node2D> indicators = CharactersContainer.GetChildren().Cast<Node2D>().ToList();

		for (int i = 0; i < indicators.Count; i++)
		{
			Node2D indicator = indicators[i];
            
			if (i >= characters.Count)
			{
				indicator.Hide();
				continue;
			}
			
			Character character = characters[i];
			
			indicator.Position = character.Position / Game.Config.tileSize;
			indicator.Modulate = character.PlayerId == Game.Player.id ? PlayerColor : OpponentColor;
		}
	}
	

	private void AdjustZoom()
	{
		Vector2 textureSize = Sprite.Texture.GetSize();
		
		float scale = textureSize.X / SubViewport.Size.X > textureSize.Y / SubViewport.Size.Y
			? SubViewport.Size.X / textureSize.X
			: SubViewport.Size.Y / textureSize.Y;
		
		Camera.Zoom = Vector2.One * scale;
	}
	
	
	public void Generate(long[,] grid, Vector2 center)
	{
		MapSize = new Vector2I(
			grid.GetLength(1),
			grid.GetLength(0)
		);
	
		Center = center;
		Offset = new Vector2(
			Center.X % 1 == 0 ? 0.5f : 0,
			Center.Y % 1 == 0 ? 0.5f : 0
		);
	
		CharactersContainer.Position = -center;
        
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
				if (grid[y, x] > 0)
				{
					Color color = TilesManager.GetColor(grid[y, x]);
					image.SetPixel(Margin + x, Margin + y, color);
				}
			}
		}
	
		Sprite.Texture = ImageTexture.CreateFromImage(image);
	
		// Has to run after the first draw
		Callable.From(AdjustZoom).CallDeferred();
	}
}
