using System;
using Godot;
using TD.Models;

namespace TD.Lists;

public partial class TracksListItem : Control
{
	private Label NameLabel;
	private Label LastEditLabel;
	public Button LoadButton;
	public Button DeleteButton;


	public override void _Ready()
	{
		base._Ready();
		NameLabel = GetNode<Label>("%NameLabel");
		LastEditLabel = GetNode<Label>("%LastEditLabel");
		LoadButton = GetNode<Button>("%LoadButton");
		DeleteButton = GetNode<Button>("%DeleteButton");
	}


	public void SetTrack(TrackInfo info)
	{
		long createdAtMs = new DateTimeOffset(info.createdAt).ToUnixTimeMilliseconds();
		
		NameLabel.Text = info.name;
		LastEditLabel.Text = $"Edited: {TimeAgo(createdAtMs)}";
	}
	
	
	public static string TimeAgo(long unixTimestampMs)
	{
		long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		long differenceInSeconds = (now - unixTimestampMs) / 1000;

		if (differenceInSeconds < 60 * 2)
			return $"{differenceInSeconds} seconds ago";

		long differenceInMinutes = differenceInSeconds / 60;
		if (differenceInMinutes < 60 * 2)
			return $"{differenceInMinutes} minutes ago";

		long differenceInHours = differenceInMinutes / 60;
		if (differenceInHours < 24 * 2)
			return $"{differenceInHours} hours ago";

		long differenceInDays = differenceInHours / 24;
		return $"{differenceInDays} days ago";
	}
}
