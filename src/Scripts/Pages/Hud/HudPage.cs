using Godot;
using System;
using TD.Models;
using TD.Pages.MainMenu;
using TD.Pages.TrackEditor;
using TD.Entities;
using TD.Enums;

namespace TD.Pages.Hud;

public class HudData
{
	public Race race;
}

public partial class HudPage : Page
{
	public static PackedScene Scene = GD.Load<PackedScene>("res://Scenes/Pages/Hud/HudPage.tscn");

	
	private Minimap Minimap;
	private RichTextLabel FinishList;
	private Label TestingTrackLabel;

	private HudData Data;


	public override void _Ready()
	{
		base._Ready();
		Minimap = GetNode<Minimap>("%Minimap");
		FinishList = GetNode<RichTextLabel>("%FinishList");
		TestingTrackLabel = GetNode<Label>("%TestingTrackLabel");
		
		FinishList.Text = string.Empty;
		TestingTrackLabel.Text = null;
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		Stage.PlayerFinished += OnPlayerFinished;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		Stage.PlayerFinished -= OnPlayerFinished;
	}


	public override void SetData(object data)
	{
		Data = (HudData)data ?? new HudData();

		if (Data.race is null)
			return;
		
		Stage.StartRace(Data.race);

		Rect2I rect = Stage.TileGrid.GetUsedRect();
		Vector2 center = new Vector2(
			rect.Position.X + rect.Size.X / 2f,
			rect.Position.Y + rect.Size.Y / 2f
		);
        
		Minimap.Generate(Stage.TileGrid.GetIdsGrid(), center);

		if (Data.race.type == RaceType.Test)
			TestingTrackLabel.Text = $"Testing track: \"{Data.race.track.name}\"";
	}
	

	private void OnQuitButtonPressed()
	{
		if (Data.race.type == RaceType.Test)
		{
			Game.SetPage(TrackEditorPage.Scene);
			return;
		}
			
		Game.SetPage(MainMenuPage.Scene);
	}

	private void OnPlayerFinished(Character character, long finishTime)
	{
		TimeSpan span = TimeSpan.FromMilliseconds(finishTime);
		string formattedTime = $"{span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
		FinishList.Text += $"{formattedTime} - {character.PlayerName}\n";
	}
}
