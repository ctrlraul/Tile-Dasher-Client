using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TD.Enums;
using TD.Extensions;
using TD.Lib;
using TD.Models;

namespace TD;

public class Cell
{
	public Tile tile;
	public Sprite2D sprite;
}

public partial class TileGrid : Node2D
{
	private static readonly Logger Logger = new("TileGrid"); 
	private readonly Dictionary<Vector2I, Cell> Data = new();
	private readonly Dictionary<Vector2I, CollisionShape2D> IndividualCollisionShapes = new();

	private Node2D SpritesContainer;
	private StaticBody2D CollisionBody;
	
	private Rect2I Rect;
	private bool RectReady;
	
	private bool[,] Grid;
	private bool GridReady;


	public override void _Ready()
	{
		base._Ready();
		SpritesContainer = GetNode<Node2D>("%SpritesContainer");
		CollisionBody = GetNode<StaticBody2D>("%CollisionBody");
	}


	public void Clear()
	{
		SpritesContainer.QueueFreeChildren();
		CollisionBody.QueueFreeChildren();
		
		Data.Clear();
		IndividualCollisionShapes.Clear();
		
		RectReady = false;
		GridReady = false;
	}
	
	public void SetTile(Vector2I coord, Tile tile)
	{
		if (tile is { id: 0 })
			tile = null;
        
		GridReady = false;
		RectReady = false;
        
		if (tile is null)
		{
			RemoveTile(coord);
			return;
		}
		
		if (Data.ContainsKey(coord))
		{
			Data[coord].tile = tile;
			Data[coord].sprite.Texture = Game.GetTileTexture(tile);
			return;
		}
		
		Sprite2D sprite = new()
		{
			Texture = Game.GetTileTexture(tile),
			Position = coord * Game.Config.tileSize
		};
	
		Data.Add(coord, new Cell
		{
			tile = tile,
			sprite = sprite
		});
	
		SpritesContainer.AddChild(sprite);
	}
	
	/// <summary>
	/// Won't remove the collision for the tile if it's static
	/// </summary>
	/// <param name="coord"></param>
	public void RemoveTile(Vector2I coord)
	{
		if (Data.TryGetValue(coord, out Cell cell))
		{
			cell.sprite.QueueFree();
			Data.Remove(coord);
		}
		
		if (IndividualCollisionShapes.TryGetValue(coord, out CollisionShape2D shape))
		{
			shape.QueueFree();
			IndividualCollisionShapes.Remove(coord);
		}
	}

	public Tile GetTile(Vector2I coord)
	{
		return Data.GetValueOrDefault(coord)?.tile;
	}

	/// <summary>
	/// Similar to GetTile but also returns null if the tile's collision shape is disabled
	/// </summary>
	public Tile GetTileTouchable(Vector2I coord)
	{
		if (IndividualCollisionShapes.TryGetValue(coord, out CollisionShape2D shape) && shape.Disabled)
			return null;
        
		return Data.GetValueOrDefault(coord)?.tile;
	}

	public Sprite2D GetSprite(Vector2I coord)
	{
		return Data.GetValueOrDefault(coord)?.sprite;
	}

	public CollisionShape2D GetCollisionShape(Vector2I coord)
	{
		return IndividualCollisionShapes.GetValueOrDefault(coord);
	}
	

	public IEnumerable<Vector2I> GetUsedCells()
	{
		return Data.Keys;
	}

	public Rect2I GetUsedRect()
	{
		if (!RectReady)
		{
			if (Data.Count == 0)
			{
				Rect = new Rect2I();
			}
			else
			{
				Vector2 topLeft = Vector2.Inf;
				Vector2 bottomRight = Vector2.Inf * -1;

				foreach (var (x, y) in Data.Keys)
				{
					topLeft.X = Math.Min(topLeft.X, x);
					topLeft.Y = Math.Min(topLeft.Y, y);
					bottomRight.X = Math.Max(bottomRight.X, x);
					bottomRight.Y = Math.Max(bottomRight.Y, y);
				}

				Vector2I position = new Vector2I(
					(int)topLeft.X,
					(int)topLeft.Y
				);

				Vector2I size = new Vector2I(
					(int)(bottomRight.X - topLeft.X),
					(int)(bottomRight.Y - topLeft.Y)
				);

				Rect = new Rect2I(position, size);
			}
			
			RectReady = true;
		}

		return Rect;
	}
	
	public bool[,] GetGrid()
	{
		if (!GridReady)
		{
			Rect2I rect = GetUsedRect();
		
			Grid = new bool[rect.Size.Y + 1, rect.Size.X + 1];

			foreach (Vector2I coord in Data.Keys)
			{
				if (Data[coord].tile.matter == Matter.Stone)
					Grid[coord.Y - rect.Position.Y, coord.X  - rect.Position.X] = true;
			}

			GridReady = true;
		}

		return Grid;
	}
	
	
	public void GenerateCollisionShapes()
	{
		CollisionBody.QueueFreeChildren();
		IndividualCollisionShapes.Clear();
        
		Rect2I rect = GetUsedRect();
		Vector2 offset = GetUsedRect().Position * -1 + Vector2.One * 0.5f;
		bool[,] solidGrid = new bool[rect.Size.Y + 1, rect.Size.X + 1];
		List<Vector2I> individualTiles = new();
		
		foreach (Vector2I coord in Data.Keys)
		{
			Tile tile = Data[coord].tile;

			if (TilesManager.IsStatic(tile))
			{
				bool[,] grid = tile.matter switch
				{
					Matter.Air => null,
					Matter.Stone => solidGrid,
					_ => null
				};
				
				if (grid is not null)
					grid[coord.Y - rect.Position.Y, coord.X  - rect.Position.X] = true;
			}
			else
			{
				individualTiles.Add(coord);
			}
		}
		
		foreach (Vector2[] polygon in GridToPoly.Generate(solidGrid))
		{
			CollisionPolygon2D collisionPolygon = new();
			CollisionBody.AddChild(collisionPolygon);
			collisionPolygon.Polygon = polygon.Select(point => (point - offset) * Game.Config.tileSize).ToArray();
		}
		
		foreach (Vector2I coord in individualTiles)
		{
			CollisionShape2D collisionShape = new();
			CollisionBody.AddChild(collisionShape);
			collisionShape.Position = coord * Game.Config.tileSize;
			collisionShape.Shape = new RectangleShape2D { Size = Vector2.One * Game.Config.tileSize };
			IndividualCollisionShapes.Add(coord, collisionShape);
		}
	}


	public void BumpAnimation(Vector2I coord)
	{
		if (!Data.TryGetValue(coord, out Cell cell))
		{
			Logger.Log("Tried to animate bump at empty coord:", coord);
			return;
		}

		Tween tween = cell.sprite.CreateTween();
		cell.sprite.Offset = cell.sprite.Offset with { Y = Game.Config.tileSize * -0.25f };
		tween.TweenProperty(cell.sprite, "offset:y", 0, 0.2);
	}
}
