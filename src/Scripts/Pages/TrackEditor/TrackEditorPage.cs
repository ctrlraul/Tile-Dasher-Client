using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TD.Connection;
using TD.Models;
using TD.Extensions;
using TD.Pages.Hud;
using TD.Pages.MainMenu;
using TD.Popups;

namespace TD.Pages.TrackEditor;

public partial class TrackEditorPage : Page
{
	public static PackedScene Scene = GD.Load<PackedScene>("res://Scenes/Pages/TrackEditor/TrackEditorPage.tscn");

	[Export] private PackedScene TileButtonScene;

	private LineEdit TrackNameInput;
	private Control TileButtonsList;
	
	private static Tile CurrentTile;
	private static Track TrackCache; // Used to remember the track after testing it
	private Vector2 cameraVelocity;
	private bool placingTiles;
	private bool hasUnsavedChanges;


	public override void _Ready()
	{
		base._Ready();
		
		TrackNameInput = GetNode<LineEdit>("%TrackNameInput");
		TileButtonsList = GetNode<Control>("%TileButtonsList");

		UpdateTileButtons();
		
		Stage.Camera.Position = Vector2.Zero;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
		Vector2 direction = GetCameraMovementKeyboardInput();
		cameraVelocity += direction * 5;
		Stage.Camera.Position += cameraVelocity;
		cameraVelocity *= 0.9f;
		
		if (placingTiles && cameraVelocity.Length() > 0.01)
			Stage.TileGrid.SetTile(Stage.TilePreview.Coord, CurrentTile);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		switch (@event)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButtonEvent:
			{
				placingTiles = mouseButtonEvent.Pressed;
			
				if (placingTiles)
					Stage.TileGrid.SetTile(Stage.TilePreview.Coord, CurrentTile);
				break;
			}
			
			case InputEventMouseMotion:
			{
				if (placingTiles)
					Stage.TileGrid.SetTile(Stage.TilePreview.Coord, CurrentTile);
				break;
			}
		}
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		Stage.TilePreview.Show();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		Stage.TilePreview.Hide();
	}


	public override void Refresh()
	{
		base.Refresh();
        
		if (CurrentTile is null)
			SetCurrentTile(Game.Tiles[0]);
		
		if (TrackCache is null)
			NewTrack();
		else
			LoadTrack(TrackCache);
	}

	private void UpdateTileButtons()
	{
		TileButtonsList.QueueFreeChildren();
		
		foreach (Tile tile in Game.Tiles)
		{
			if (!tile.listed)
				continue;
            
			TileButton button = TileButtonScene.Instantiate<TileButton>();
			TileButtonsList.AddChild(button);
			button.SetTile(tile);
			button.Pressed += () => SetCurrentTile(tile);
		}
	}
	

	private void SetCurrentTile(Tile tile)
	{
		CurrentTile = tile;
		
		foreach (TileButton button in TileButtonsList.GetChildren().Cast<TileButton>())
			button.SetSelected(button.Tile == CurrentTile);
		
		Stage.TilePreview.SetTile(CurrentTile);
	}
	
	private async Task<string> SaveTrackLocally()
	{
		Track track = Stage.ExportTrack();
		
		string folderPath = ProjectSettings.GlobalizePath(Game.Config.tracksFolder);
		string filePath = folderPath.PathJoin(track.id + ".json");
		string json = JsonConvert.SerializeObject(track);
		
		Directory.CreateDirectory(folderPath);
		
		await File.WriteAllTextAsync(filePath, json);

		return filePath;
	}

	private Vector2 GetCameraMovementKeyboardInput()
	{
		Vector2 direction = Vector2.Zero;

		if (Input.IsActionPressed("move_left"))
			direction.X -= 1;
		if (Input.IsActionPressed("move_right"))
			direction.X += 1;
		if (Input.IsActionPressed("jump"))
			direction.Y -= 1;
		if (Input.IsActionPressed("crouch"))
			direction.Y += 1;

		return direction.Normalized();
	}

	private async void LoadTrack(string trackId)
	{
		Task<Result<Track>> task = Socket.SendGetTrack(trackId);

		if (!task.IsCompleted)
		{
			await PopupsManager.PleaseWait(task, "Loading track...");
			
			if (task.Result.error != null)
			{
				PopupsManager.GenericErrorDialog("Error loading track!", task.Result.error);
				return;
			}
		}

		LoadTrack(task.Result.data);
	}

	private void LoadTrack(Track track)
	{
		TrackNameInput.Text = track.name;
		Stage.LoadTrackToEdit(track);
	}

	private Track ExportTrack()
	{
		Track track = Stage.ExportTrack();
		track.name = TrackNameInput.Text;
		return track;
	}

	private void NewTrack()
	{
		TrackNameInput.Text = "New Track " + GenerateRandomCode();
		Stage.Clear();
	}
	
	public static string GenerateRandomCode()
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		Random random = new();
		char[] result = new char[4];

		for (int i = 0; i < result.Length; i++)
		{
			result[i] = chars[random.Next(chars.Length)];
		}

		return new string(result);
	}


	public void OnReturnButtonPressed()
	{
		Game.SetPage(MainMenuPage.Scene);
	}

	private void OnLoadButtonPressed()
	{
		TracksPopup popup = PopupsManager.SelectTrack();
		popup.Selected += LoadTrack;

		foreach (TrackInfo info in Game.Player.trackInfos)
			popup.AddTrack(info);
	}

	private async void OnSaveButtonPressed()
	{
		Track track = ExportTrack();
        
		try
		{
			Task<Result<Track>> task = Game.Player.trackInfos.Any(info => info.id == track.id)
				? Socket.SendTrackUpdate(track)
				: Socket.SendTrackCreate(track);
			
			await PopupsManager.PleaseWait(task, "Saving track...");
            
			if (task.Result.error is not null)
				throw new Exception(task.Result.error);
			
			// string filePath = await SaveTrackLocally();
			//
			// PopupsManager.Dialog()
			// 	.SetTitle("Track saved!")
			// 	.SetMessage("File: " + filePath)
			// 	.AddButton("Ok")
			// 	.AddButton("Open folder", () => OS.ShellShowInFileManager(filePath))
			// 	.SetCancellable(true);
		}
		catch (Exception exception)
		{
			PopupsManager.GenericErrorDialog("Error saving track!", exception.Message);
		}
	}

	private void OnNewButtonPressed()
	{
		PopupsManager.Dialog()
			.SetTitle("New Track")
			.SetMessage("Don't do this if you have unsaved changes you want to keep")
			.AddButton("Cancel")
			.AddButton("Ok", NewTrack)
			.SetCancellable(true);
	}

	private void OnTestButtonPressed()
	{
		TrackCache = ExportTrack();
		
		HudData hudData = new()
		{
			track = TrackCache,
			testing = true
		};
		
		Game.SetPage(HudPage.Scene, hudData);
	}
}