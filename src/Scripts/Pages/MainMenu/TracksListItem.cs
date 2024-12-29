using Godot;
using System;
using TD.Models;

namespace TD.Pages.MainMenu;

public partial class TracksListItem : Control
{
	public Button Button;
	private Label TrackNameLabel;
	private Label PlayerNameLabel;
	private Label PlaysLabel;


	public override void _Ready()
	{
		base._Ready();
		Button = GetNode<Button>("%Button");
		TrackNameLabel = GetNode<Label>("%TrackNameLabel");
		PlayerNameLabel = GetNode<Label>("%PlayerNameLabel");
		PlaysLabel = GetNode<Label>("%PlaysLabel");
	}


	public void SetTrackInfo(TrackInfo trackInfo)
	{
		TrackNameLabel.Text = trackInfo.name;
		PlayerNameLabel.Text = "";
		PlaysLabel.Text = $"{trackInfo.plays} plays";
	}
}
