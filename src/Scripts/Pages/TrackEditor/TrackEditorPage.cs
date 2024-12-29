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

public class TrackEditorData
{
	public Track track;
}

public partial class TrackEditorPage : Page
{
	public static PackedScene Scene = GD.Load<PackedScene>("res://Scenes/Pages/TrackEditor/TrackEditorPage.tscn");

	[Export]
	private PackedScene TileButtonScene;

	private Control TileButtonsList;
	
	private Vector2 cameraVelocity;
	private Tile currentTile;
	private bool placingTiles;

	private TrackEditorData Data;


	public override void _Ready()
	{
		base._Ready();
		TileButtonsList = GetNode<Control>("%TileButtonsList");

		TileButtonsList.QueueFreeChildren();
		foreach (Tile tile in Server.Tiles)
		{
			TileButton button = TileButtonScene.Instantiate<TileButton>();
			TileButtonsList.AddChild(button);
			button.SetTile(tile);
			button.Pressed += () => SetCurrentTile(tile);
		}
		
		Stage.Camera.Position = Vector2.Zero;
		
		SetCurrentTile(Server.Tiles[0]);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
		Vector2 direction = GetCameraMovementKeyboardInput();
		cameraVelocity += direction * 5;
		Stage.Camera.Position += cameraVelocity;
		cameraVelocity *= 0.9f;
		
		if (placingTiles && cameraVelocity.Length() > 0.01)
			Stage.TileGrid.SetTile(Stage.TilePreview.Coord, currentTile);
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
					Stage.TileGrid.SetTile(Stage.TilePreview.Coord, currentTile);
				break;
			}
			
			case InputEventMouseMotion:
			{
				if (placingTiles)
					Stage.TileGrid.SetTile(Stage.TilePreview.Coord, currentTile);
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


	public override void SetData(object data)
	{
		Data = (TrackEditorData)data ?? new TrackEditorData();

		if (Data.track is not null)
			Stage.LoadTrack(Data.track);
	}
	

	private void SetCurrentTile(Tile tile)
	{
		currentTile = tile;
		
		foreach (TileButton button in TileButtonsList.GetChildren().Cast<TileButton>())
			button.SetSelected(button.Tile == currentTile);
		
		Stage.TilePreview.SetTile(currentTile);
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


	public void OnReturnButtonPressed()
	{
		Game.SetPage(MainMenuPage.Scene);
	}

	private async void OnLoadButtonPressed()
	{
		Result<Track> result = await Server.SendGetTrack("0193f7d5-2c66-758d-9ac5-ab7f858aaaea");

		if (result.error == null)
			Stage.LoadTrack(result.data);
	}

	private async void OnExportButtonPressed()
	{
		Track track = Stage.ExportTrack();
        
		try
		{
			Result<Track> result;

			if (Server.Player.trackInfos.Any(info => info.id == track.id))
				result = await Server.SendTrackUpdate(track);
			else
				result = await Server.SendTrackCreate(track);

			if (result.error is not null)
				throw new Exception(result.error);
			
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
			PopupsManager.GenericErrorDialog("Error exporting track!", exception.Message);
		}
	}

	private void OnClearButtonPressed()
	{
		Stage.Clear();
	}

	private void OnTestButtonPressed()
	{
		HudData hudData = new()
		{
			track = Stage.ExportTrack(),
			testing = true,
		};
		
		Game.SetPage(HudPage.Scene, hudData);
	}
}