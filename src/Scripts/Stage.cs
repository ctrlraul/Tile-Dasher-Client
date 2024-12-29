using System;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Costasdev.Uuidv7;
using TD.Connection;
using TD.Enums;
using TD.Models;
using TD.Exceptions;
using TD.Extensions;
using TD.Lib;
using TD.Pages.TrackEditor;
using TD.TileEffects;

namespace TD;

public partial class Stage : Node2D
{
	public static Stage Instance { get; private set; }
	private static readonly Logger Logger = new(nameof(Stage));

	[Export] public PackedScene CharacterScene;

	public static TileGrid TileGrid;
	private static Node2D CharactersContainer;
	public static Camera2D Camera { get; private set; }
	private static Node2D LowerBoundaryIndicator;
	public static TilePreview TilePreview;
	public static TileBehaviors TileBehaviors; // TODO
	public static Node2D Temp;

	private static Node2D CameraTarget;
	private static Vector2 CameraVelocity;
	private static string trackId;
	
	public static float LowerBoundary { get; private set; }


	public override void _Ready()
	{
		base._Ready();

		if (Instance != null)
			throw new SingletonException(typeof(Stage));

		Instance = this;
		
		TileGrid = GetNode<TileGrid>("%TileGrid");
		CharactersContainer = GetNode<Node2D>("%CharactersContainer");
		Camera = GetNode<Camera2D>("%Camera");
		LowerBoundaryIndicator = GetNode<Node2D>("%LowerBoundaryIndicator");
		TilePreview = GetNode<TilePreview>("%TilePreview");
		TileBehaviors = GetNode<TileBehaviors>("%TileBehaviors");
		Temp = GetNode<Node2D>("%Temp");

		TilePreview.Hide();
		LowerBoundaryIndicator.Hide();
		
		Clear();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (IsInstanceValid(CameraTarget))
			CameraVelocity = (CameraTarget.GlobalPosition - Camera.GlobalPosition) * 0.1f;

		Camera.GlobalPosition += CameraVelocity;
		CameraVelocity *= 0.9f;
	}


	public static void Clear()
	{
		CameraTarget = null;
		trackId = null;
		CharactersContainer.QueueFreeChildren();
		Temp.QueueFreeChildren();
		TileGrid.Clear();
		LowerBoundary = float.PositiveInfinity;
	}


	public static void LoadTrack(Track track)
	{
		Clear();
		
		trackId = track.id;
		
		foreach (long id in track.tileCoords.Keys)
		{
			List<int> coords = track.tileCoords[id];
			Tile tile = Server.Tiles.Find(tile => tile.id == id);
			
			for (int i = 0; i < coords.Count; i += 2)
			{
				Vector2I coord = new(coords[i], coords[i + 1]);
				TileGrid.SetTile(coord, tile);
			}
		}
	}

	public static void PlayTrack(Track track)
	{
		LoadTrack(track);

		Stopwatch stopwatch = Stopwatch.StartNew();
		TileGrid.GenerateCollisionShapes();
		Logger.Log($"TileGrid.GenerateCollisionShapes() - {stopwatch.ElapsedMilliseconds}ms");
		stopwatch.Stop();
		
		Rect2 rect = TileGrid.GetUsedRect();
		LowerBoundary = (rect.Position.Y + rect.Size.Y + 24) * Game.Config.tileSize;
		LowerBoundaryIndicator.Position = Vector2.Down * LowerBoundary;

		Character player = Instance.CharacterScene.Instantiate<Character>();
		CharactersContainer.AddChild(player);
		player.SetPlayerName(Server.Player.name);
		player.RespawnPoint = GetSpawnPosition(track);
		player.Position = player.RespawnPoint;

		CameraTarget = player;
	}

	public static Track ExportTrack()
	{
		Track track = new()
		{
			id = trackId ?? Uuid7.NewUuid().ToString(),
			name = "My Track",
			customTiles = new Dictionary<long, Tile>(),
			tileCoords = new Dictionary<long, List<int>>()
		};
		
		foreach (Vector2I coord in TileGrid.GetUsedCells())
		{
			Tile tile = TileGrid.GetTile(coord);

			if (track.tileCoords.ContainsKey(tile.id))
			{
				track.tileCoords[tile.id].Add(coord.X);
				track.tileCoords[tile.id].Add(coord.Y);
			}
			else
			{
				track.tileCoords.Add(tile.id, new List<int> { coord.X, coord.Y });
			}
		}

		return track;
	}

	public static IEnumerable<Character> GetCharacters()
	{
		return CharactersContainer.GetChildren().Cast<Character>();
	}

	public static Vector2 GetSpawnPosition(Track track)
	{
		if (track.tileCoords.TryGetValue(Game.Config.spawnTileId, out List<int> coords))
		    return new Vector2(coords[0], coords[1]) * Game.Config.tileSize;
			
		return Vector2.Zero;
	}


	public static Vector2I WorldToGrid(Vector2 position)
	{
		return new Vector2I(
			(int)Math.Round(position.X / Game.Config.tileSize),
			(int)Math.Round(position.Y / Game.Config.tileSize)
		);
	}

	public static Vector2 GridToWorld(Vector2I position)
	{
		return position * Game.Config.tileSize;
	}


	public static void TriggerTileEffects(string trigger, Tile tile, Vector2I coord, Character character)
	{
		if (tile is null)
		{
			Logger.Log("TriggerTileEffects :: Tile is null");
			return;
		}
		
		if (tile.effects.TryGetValue(trigger, out List<string> effectIds))
		{
			foreach (ITileEffect effect in effectIds.Select(id => Game.TileEffects[id]))
				effect.Trigger(tile, coord, character);
		}
		else if (trigger == TileEffectTrigger.Bump)
		{
			TileGrid.BumpAnimation(coord);
		}
	}
}
