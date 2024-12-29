using Godot;
using System;
using TD.Models;
using TD.Pages.MainMenu;
using TD.Pages.TrackEditor;

namespace TD.Pages.Hud;

public class HudData
{
	public Track track;
	public bool testing;
}

public partial class HudPage : Page
{
	public static PackedScene Scene = GD.Load<PackedScene>("res://Scenes/Pages/Hud/HudPage.tscn");

	
	private static Minimap Minimap;
	private Label TestingTrackLabel;

	private HudData Data;


	public override void _Ready()
	{
		base._Ready();
		Minimap = GetNode<Minimap>("%Minimap");
		TestingTrackLabel = GetNode<Label>("%TestingTrackLabel");
		TestingTrackLabel.Text = null;
		TreeExiting += Stage.Clear;
	}


	public override void SetData(object data)
	{
		Data = (HudData)data ?? new HudData();

		if (Data.track is null)
			return;
		
		Stage.PlayTrack(Data.track);

		Rect2I rect = Stage.TileGrid.GetUsedRect();
		Minimap.Generate(Stage.TileGrid.GetGrid(), rect.Position + rect.Size / 2);

		if (Data.testing)
			TestingTrackLabel.Text = $"Testing track: \"{Data.track.name}\"";
	}


	public void OnQuitButtonPressed()
	{
		if (Data.testing)
		{
			Game.SetPage(TrackEditorPage.Scene, new TrackEditorData
			{
				track = Data.track
			});

			return;
		}
			
		Game.SetPage(MainMenuPage.Scene);
	}
}
