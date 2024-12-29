using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using TD.Connection;
using TD.Exceptions;
using TD.Lib;
using TD.Pages;
using TD.Pages.MainMenu;
using TD.Pages.Welcome;
using Newtonsoft.Json;
using TD.Models;
using TD.TileEffects;

namespace TD;

public partial class Game : Node
{
	private const string DevConfigFileName = "config.dev.json";

	[Export] private Texture2D MissingTexture;
	[Export] private Texture2D TileSet;
	[Export] private PackedScene DotScene;
    
	private static Game Instance;
	private static readonly Logger Logger = new("Game");
	private static int InstanceIndex;

	private static CanvasLayer ReconnectingOverlay;

	public static Config Config { get; private set; }
	public static Node CurrentScene => Instance.GetTree().CurrentScene;
	public static bool InDev => OS.HasFeature("editor");
	
	public static string ScenePathMain { get; } = ProjectSettings.GetSetting("application/run/main_scene").AsString();
	public static string ScenePathLaunch { get; private set; }
	
	public static readonly Dictionary<string, ITileEffect> TileEffects = new();
	
	
	public override void _Ready()
	{
		if (Instance != null)
			throw new SingletonException(GetType());

		Instance = this;
		ScenePathLaunch = CurrentScene.SceneFilePath;
		ReconnectingOverlay = GetNode<CanvasLayer>("%ReconnectingOverlay");

		Server.LoggedIn += OnLoggedIn;
		Server.LoggedOut += OnLoggedOut;
		Server.Connected += OnConnected;
		Server.Disconnected += OnDisconnected;
		Server.SseInitialData += OnSseInitialData;

		LoadInstanceIndex();
		LoadConfig();
		
		TileEffects.Add("break", new TileEffectBreak());
		TileEffects.Add("vanish", new TileEffectVanish());
		
		CallDeferred(MethodName.AfterReady);
		
		Logger.Log($"OS - {OS.GetName()}");
	}
	
	
	private static void AfterReady()
	{
		Server.Initialize(Config.serverUrl);
		
		if (ScenePathLaunch != ScenePathMain)
		{
			Logger.Log($"Not launched from main scene - {nameof(Initialize)} will not be called to not interrupt F6 testing!");
			return;
		}
		
		Initialize();
	}

	private static async void Initialize()
	{
		try
		{
			if (InstanceIndex != -1)
				await Server.LoginAsPlayer(Config.multiplayerTestPlayerIds[InstanceIndex]);
			else
				await Server.UpdateLoggedInState();
			
			if (Server.IsLoggedIn)
				OnLoggedIn();
			else
				SetPage(WelcomePage.Scene);
		}
		catch (Exception exception)
		{
			Logger.Log($"Failed to launch: {exception.Message}");
		}
	}
	
	private static void LoadInstanceIndex()
	{
		const string prefix = "instance=";
		
		foreach (string arg in OS.GetCmdlineArgs())
		{
			if (arg.StartsWith(prefix))
			{
				InstanceIndex = int.Parse(arg.Substring(prefix.Length));
				Logger.GlobalPrefix = $"{InstanceIndex} ";
				return;
			}
		}

		InstanceIndex = -1;
	}
	
	private static void LoadConfig()
	{
		if (InDev)
		{
			string devConfigFilePath = ProjectSettings.GlobalizePath("user://" + DevConfigFileName);

			if (File.Exists(devConfigFilePath))
			{
				try
				{
					string json = File.ReadAllText(devConfigFilePath);
					Config = JsonConvert.DeserializeObject<Config>(json);
					Logger.Log("Config: Dev");
					return;
				}
				catch (Exception exception)
				{
					Logger.Log($"Error loading dev config file: {exception.Message}");
				}
			}
		}
		
		Config = new Config();
		Logger.Log("Config: Default");
	}
	
	
	public static void SetPage(PackedScene pageScene, object data = null)
	{
		SceneTree tree = Instance.GetTree();
		Page page = pageScene.Instantiate<Page>();
		
		tree.Root.AddChild(page);
		
		tree.CurrentScene.TreeExited += () => page.SetData(data);
		tree.CurrentScene.QueueFree();
		tree.CurrentScene = page;
	}

	public static Texture2D GetTileTexture(Tile tile)
	{
		return new AtlasTexture
		{
			Atlas = Instance.TileSet,
			Region = new Rect2(tile.AtlasCoords * Config.tileSize, new Vector2(Config.tileSize, Config.tileSize)),
		};
	}

	public static Node2D AddDot(Vector2 position)
	{
		Node2D dot = Instance.DotScene.Instantiate<Node2D>();
		dot.Position = position;
		Stage.Temp.AddChild(dot);
		return dot;
	}
    

	private static void OnLoggedIn()
	{
		_ = Server.Connect(InDev ? 1000 : 0);
	}

	private static void OnLoggedOut()
	{
		SetPage(WelcomePage.Scene);
	}

	private static void OnConnected()
	{
		// Do stuff in OnSseInitialData instead since that's when we know it's safe to access stuff
	}

	private static void OnDisconnected()
	{
		Logger.Log("Disconnected");
		
		if (Server.IsLoggedIn)
			ReconnectingOverlay.Show();
	}

	private static void OnSseInitialData()
	{
		Logger.Json(Server.Player);
		TilesManager.SetTiles(Server.Tiles);
        
		// It's the first connection (just opened the game)
		if (Server.DisconnectionsSinceLogin == 0)
		{
			SetPage(MainMenuPage.Scene);
			
			ReconnectingOverlay.Hide();
			return;
		}


		// Refresh the current page in case it relies on data that has changed
		if (CurrentScene is Page currentPage)
			currentPage.Refresh();
		

		ReconnectingOverlay.Hide();
	}
}