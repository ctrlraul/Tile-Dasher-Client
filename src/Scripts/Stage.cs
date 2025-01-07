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
using TD.Particles;
using TD.TileEffects;

namespace TD;

public partial class Stage : Node2D
{
	public static event Action<Character, long> PlayerFinished;
	
	public static Stage Instance { get; private set; }
	private static readonly Logger Logger = new(nameof(Stage));

	[Export] public PackedScene CharacterScene;
	[Export] public PackedScene CrumbleParticlesScene;

	public static TileGrid TileGrid;
	private static Node2D CharactersContainer;
	public static Camera2D Camera { get; private set; }
	private static Node2D LowerBoundaryIndicator;
	public static TilePreview TilePreview;
	public static Node2D Temp;

	private static Vector2 CameraVelocity;
	private static string trackId;
	private static long StartTime;
	
	public static float LowerBoundary { get; private set; }
	public static readonly Dictionary<long, List<Vector2I>> TilesIndex = new();


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
		Temp = GetNode<Node2D>("%Temp");

		TilePreview.Hide();
		LowerBoundaryIndicator.Hide();
		
		Clear();
	}

	private Vector2 CameraSeek;

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		Camera.Position += CameraVelocity;
		CameraVelocity *= 0.9f;
	}


	public static void Clear()
	{
		trackId = null;
		CharactersContainer.QueueFreeChildren();
		Temp.QueueFreeChildren();
		TileGrid.Clear();
		LowerBoundary = float.PositiveInfinity;
		TilesIndex.Clear();
	}


	public static void LoadTrackToEdit(Track track)
	{
		Clear();
		
		trackId = track.id;

		foreach (long id in track.tileCoords.Keys)
		{
			List<int> coords = track.tileCoords[id];
			Tile tile = Game.Tiles.Find(tile => tile.id == id);
			
			for (int i = 0; i < coords.Count; i += 2)
			{
				Vector2I coord = new(coords[i], coords[i + 1]);
				TileGrid.SetTile(coord, tile);
			}
		}
	}

	public static void LoadTrackToPlay(Track track)
	{
		Clear();
		
		trackId = track.id;

		List<(TileEffect, Tile, Vector2I)> onInitList = new();
		
		
		foreach (long id in track.tileCoords.Keys)
		{
			List<int> coords = track.tileCoords[id];
			Tile tile = Game.Tiles.Find(tile => tile.id == id);
			
			for (int i = 0; i < coords.Count; i += 2)
			{
				Vector2I coord = new(coords[i], coords[i + 1]);
				TileGrid.SetTile(coord, tile);

				if (TilesManager.NeedsIndexing(tile))
				{
					if (TilesIndex.TryGetValue(tile.id, out List<Vector2I> value))
						value.Add(coord);
					else
						TilesIndex.Add(tile.id, [coord]);
				}

				foreach (TileEffectConfig config in tile.effects)
				{
					if (config.trigger == TileEffectTrigger.Init)
					{
						TileEffect effect = TilesManager.Effects[config.effect];
						onInitList.Add((effect, tile, coord));
					}
				}
			}
		}
		
		
		TileGrid.GenerateCollisionShapes();

		
		TriggerData triggerData = new()
		{
			tile = null,
			coord = Vector2I.Zero,
			character = null,
			trigger = TileEffectTrigger.Init,
		};
		
		foreach (var (effect, tile, coord) in onInitList)
		{
			triggerData.tile = tile;
			triggerData.coord = coord;
			effect.Trigger(triggerData);
		}
	}

	public static void StartRace(Race race)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		LoadTrackToPlay(race.track);
		Logger.Log($"LoadTrackToPlay() - {stopwatch.ElapsedMilliseconds}ms");
		stopwatch.Stop();
		
		Rect2 rect = TileGrid.GetUsedRect();
		LowerBoundary = (rect.Position.Y + rect.Size.Y + 24) * Game.Config.tileSize;
		LowerBoundaryIndicator.Position = Vector2.Down * LowerBoundary;

		foreach (PlayerProfile player in race.players)
		{
			Character character = Instance.CharacterScene.Instantiate<Character>();
			CharactersContainer.AddChild(character);
			character.RespawnPoint = GetSpawnPosition(race.track);
			character.Position = character.RespawnPoint;
			
			if (player.id == Game.Player.id)
				character.RemoteTransform.RemotePath = character.RemoteTransform.GetPathTo(Camera);
			else
				character.SetPlayerName(player.name);
		}
		
		StartTime = new DateTimeOffset(race.startTime).ToUnixTimeMilliseconds();
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


	public static void TriggerTileEffects(string trigger, Tile tile, Vector2I coord, Character character)
	{
		if (tile is null)
		{
			// Logger.Log("TriggerTileEffects :: Tile is null");
			return;
		}

		TriggerData triggerData = new()
		{
			tile = tile,
			coord = coord,
			character = character,
			trigger = trigger,
		};

		List<string> effectIds = new();

		foreach (TileEffectConfig config in tile.effects)
		{
			if (config.trigger == trigger || config.trigger == TileEffectTrigger.Any)
				effectIds.Add(config.effect);
		}

		if (effectIds.Any())
		{
			foreach (TileEffect effect in effectIds.Select(id => TilesManager.Effects[id]))
				effect.Trigger(triggerData);
		}
		else if (trigger == TileEffectTrigger.Bump)
		{
			TileGrid.BumpAnimation(coord);
		}
	}

	public static void Finish(Character character)
	{
		if (character.Finished)
			return;
		
		character.Finish();
		
		long finishTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - StartTime;
		PlayerFinished?.Invoke(character, finishTime);
	}

	public static void AddCrumbleParticles(Vector2I coord)
	{
		CrumbleParticles particles = Instance.CrumbleParticlesScene.Instantiate<CrumbleParticles>();
		Temp.AddChild(particles);
		particles.Init(coord);
	}
}
