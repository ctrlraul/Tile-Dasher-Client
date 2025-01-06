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
using TD.Pages.Splash;
using TD.Popups;

namespace TD;

public partial class Game : Node
{
	private const string DevConfigFileName = "config.dev.json";
	private const string DisconnectionReasonLogout = "Logout";

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
	
	
	public static Player Player => Socket.ClientData?.player;
	public static List<Tile> Tiles => Socket.ClientData?.tiles;
	
	
	public override void _Ready()
	{
		if (Instance != null)
			throw new SingletonException(GetType());

		Instance = this;
		ScenePathLaunch = CurrentScene.SceneFilePath;
		ReconnectingOverlay = GetNode<CanvasLayer>("%ReconnectingOverlay");

		AuthManager.LoggedIn += OnLoggedIn;
		AuthManager.LoggedOut += OnLoggedOut;
		Socket.Connected += OnSocketConnected;
		Socket.Disconnected += OnSocketDisconnected;
		Socket.GotClientData += OnSocketGotClientData;
		Socket.GotClientDataError += OnSocketGotClientDataError;

		LoadInstanceIndex();
		LoadConfig();
		
		CallDeferred(MethodName.AfterReady);
		
		Logger.Log($"OS - {OS.GetName()}");
	}
	
	
	private static void AfterReady()
	{
		AuthManager.Initialize(Config.serverUrl);
		
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
				await AuthManager.LoginAsPlayer(Config.multiplayerTestPlayerIds[InstanceIndex]);
			else
				await AuthManager.UpdateLoggedInState();
			
			if (AuthManager.IsLoggedIn)
				OnLoggedIn();
			else
				SetPage(WelcomePage.Scene);
		}
		catch (Exception exception)
		{
			PopupsManager.Dialog()
				.SetTitle("Launch Error!")
				.SetMessage(exception.Message)
				.SetStyle(DialogPopup.Style.Error)
				.AddButton("Quit", () => Instance.GetTree().Quit())
				.AddButton("Retry", Initialize);
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
		Stage.Clear();
        
		SceneTree tree = Instance.GetTree();
		Page page = pageScene.Instantiate<Page>();
		
		tree.Root.AddChild(page);
		
		tree.CurrentScene.TreeExited += () => page.SetData(data);
		tree.CurrentScene.QueueFree();
		tree.CurrentScene = page;
		
		Callable.From(page.Refresh).CallDeferred();
	}

	public static Texture2D GetTileTexture(Tile tile)
	{
		return TilesManager.GetTexture(tile);
	}

	public static Node2D AddDot(Vector2 position)
	{
		Node2D dot = Instance.DotScene.Instantiate<Node2D>();
		dot.Position = position;
		Stage.Temp.AddChild(dot);
		return dot;
	}

	public static Vector2I WorldToGrid(Vector2 position)
	{
		return new Vector2I(
			(int)Math.Round(position.X / Config.tileSize),
			(int)Math.Round(position.Y / Config.tileSize)
		);
	}

	public static Vector2 GridToWorld(Vector2I position)
	{
		return position * Config.tileSize;
	}

	public static Texture2D GetTileSet()
	{
		return Instance.TileSet;
	}
	

	private static void OnLoggedIn()
	{
		_ = Socket.Connect(Config.webSocketServerUrl);
	}

	private static void OnLoggedOut()
	{
		Socket.Disconnect(DisconnectionReasonLogout);
		SetPage(WelcomePage.Scene);
	}


	private static void OnSocketConnected()
	{
		Logger.Log("Socket Connected");
		// Server will automatically send client data, so from
		// here we just wait for OnSocketGotClientData to run.
	}
	
	private static void OnSocketDisconnected()
	{
		Logger.Log($"Socket Disconnected (reason: {Socket.LastDisconnectionReason})");
		
		if (Socket.LastDisconnectionReason != DisconnectionReasonLogout)
			ReconnectingOverlay.Show();
	}

	private static void OnSocketGotClientData(ClientData data)
	{
		// Logger.Json(data.player);
		
		TilesManager.SetTiles(data.tiles);

		ReconnectingOverlay.Hide();
        
		switch (CurrentScene)
		{
			case SplashPage or WelcomePage:
				SetPage(MainMenuPage.Scene);
				break;
			
			// Refresh the current page in case it relies on data that has changed
			case Page currentPage:
				currentPage.Refresh();
				break;
		}
	}

	private static void OnSocketGotClientDataError()
	{
		PopupsManager.Dialog()
			.SetTitle("Error loading client data!")
			.SetStyle(DialogPopup.Style.Error)
			.AddButton("Quit", () => Instance.GetTree().Quit())
			.AddButton("Retry", Initialize);
	}
}