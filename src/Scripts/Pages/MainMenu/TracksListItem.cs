using Godot;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TD.Connection;
using TD.Misc;
using TD.Models;
using TD.Popups;

namespace TD.Pages.MainMenu;

public partial class TracksListItem : Control
{
	private Label TrackNameLabel;
	private Label AuthorLabel;
	private Label PlaysLabel;
	private List<Control> PlayerLines;
	private InputFilter InputFilter;
	private Control Buttons;

	private TrackInfo TrackInfo;
	private bool LocalPlayerPlaying;


	public override void _Ready()
	{
		base._Ready();
		
		TrackNameLabel = GetNode<Label>("%TrackNameLabel");
		AuthorLabel = GetNode<Label>("%AuthorLabel");
		PlaysLabel = GetNode<Label>("%PlaysLabel");
		PlayerLines =
		[
			GetNode<Control>("%PlayerLine1"),
			GetNode<Control>("%PlayerLine2"),
			GetNode<Control>("%PlayerLine3"),
			GetNode<Control>("%PlayerLine4")
		];
		InputFilter = GetNode<InputFilter>("%InputFilter");
		Buttons = GetNode<Control>("%Buttons");

		InputFilter.FilterClicked += OnCancelButtonPressed;

		Clear();
	}
	
	public override void _EnterTree()
	{
		base._EnterTree();
		Socket.GotRacesQueueUpdate += OnGotRacesQueueUpdate;
	}

	public override void _ExitTree()
	{
		base._EnterTree();
		Socket.GotRacesQueueUpdate -= OnGotRacesQueueUpdate;
	}


	private void Clear()
	{
		foreach (Control playerLine in PlayerLines)
			ClearPlayerLine(playerLine);
		
		LocalPlayerPlaying = false;
        
		InputFilter.Visible = false;
		Buttons.Visible = false;
	}
    
	private void UpdatePlayerLines()
	{
		if (!Game.RacesQueue.TryGetValue(TrackInfo.id, out RacesQueueEntry rqEntry))
		{
			Clear();
			return;
		}
		
		List<PlayerProfile> players = rqEntry.players.Values.ToList();
		
		for (int i = 0; i < PlayerLines.Count; i++)
		{
			Control playerLine = PlayerLines[i];
			PlayerProfile profile = i < players.Count ? players[i] : null;

			if (profile is null)
				ClearPlayerLine(playerLine);
			else
				FillPlayerLine(playerLine, profile, rqEntry.playersReady.Contains(profile.id));
		}

		LocalPlayerPlaying = rqEntry.players.ContainsKey(Game.Player.id);
        
		InputFilter.Visible = LocalPlayerPlaying;
		Buttons.Visible = LocalPlayerPlaying;
	}

	public void SetTrackInfo(TrackInfo trackInfo)
	{
		TrackInfo = trackInfo;
		TrackNameLabel.Text = trackInfo.name;
		AuthorLabel.Text = $"By: {trackInfo.author}";
		PlaysLabel.Text = $"Plays: {trackInfo.plays}";
		UpdatePlayerLines();
	}

	private void ClearPlayerLine(Control node)
	{
		node.GetNode<Control>("Background").Modulate = Colors.Transparent;
		node.GetNode<Label>("Label").Text = "---";
	}

	private void FillPlayerLine(Control node, PlayerProfile profile, bool ready)
	{
		node.GetNode<Control>("Background").Modulate = ready ? Colors.White : Colors.Transparent;
		node.GetNode<Label>("Label").Text = profile.name;
	}


	private async void OnPressed()
	{
		if (LocalPlayerPlaying)
			return;
		
		Result result = await Socket.SendRaceQueueEnter(TrackInfo.id);

		if (result.error is not null)
			PopupsManager.GenericErrorDialog("Error entering track queue!", result.error);
	}

	private void OnCancelButtonPressed()
	{
		_ = Socket.SendRaceQueueLeave();
	}

	private void OnPlayButtonPressed()
	{
		_ = Socket.SendRaceQueueReady();
	}
    
	private void OnGotRacesQueueUpdate(string trackId)
	{
		if (trackId == TrackInfo.id)
			UpdatePlayerLines();
	}
}
