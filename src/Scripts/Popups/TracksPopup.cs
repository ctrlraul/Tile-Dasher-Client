using Godot;
using System;
using System.Threading.Tasks;
using TD.Connection;
using TD.Extensions;
using TD.Lists;
using TD.Models;

namespace TD.Popups;

public partial class TracksPopup : GenericPopup
{
	public event Action<string> Selected;

	[Export] public PackedScene TracksListItemScene;

	private Control TracksList;


	public override void _Ready()
	{
		base._Ready();
		TracksList = GetNode<Control>("%TracksList");
		TracksList.QueueFreeChildren();
		SetCancellable(true);
	}


	public void AddTrack(TrackInfo trackInfo)
	{
		TracksListItem listItem = TracksListItemScene.Instantiate<TracksListItem>();
		
		TracksList.AddChild(listItem);
		
		listItem.LoadButton.Pressed += () =>
		{
			Selected?.Invoke(trackInfo.id);
			Remove();
		};
		
		listItem.DeleteButton.Pressed += () =>
		{
			PopupsManager.Dialog()
				.SetTitle("Delete Track")
				.SetMessage("Are you sure? (No undo)")
				.AddButton("Cancel")
				.AddButton("Ok", () => DeleteTrack(listItem, trackInfo.id))
				.SetCancellable(true);
		};
		
		listItem.SetTrack(trackInfo);
	}


	private async void DeleteTrack(TracksListItem listItem, string id)
	{
		listItem.QueueFree();
		await Socket.SendTrackDelete(id);
	}
}
