using Godot;
using TD.Connection;
using TD.Extensions;
using TD.Models;
using TD.Pages.Hud;
using TD.Pages.TrackEditor;
using TD.Popups;

namespace TD.Pages.MainMenu;

public partial class MainMenuPage : Page
{
    public static readonly PackedScene Scene = GD.Load<PackedScene>("res://Scenes/Pages/MainMenu/MainMenuPage.tscn");


    [Export] private PackedScene TracksListItemScene;

    private Label PlayerNameLabel;
    private Label PlayerLevelLabel;
    private Control TracksList;
    
    
    public override void _Ready()
    {
        PlayerNameLabel = GetNode<Label>("%PlayerNameLabel");
        PlayerLevelLabel = GetNode<Label>("%PlayerLevelLabel");
        TracksList = GetNode<Control>("%TracksList");
    }
	
    public override void _EnterTree()
    {
        base._EnterTree();
        Socket.GotRaceStart += OnGotRaceStart;
    }

    public override void _ExitTree()
    {
        base._EnterTree();
        Socket.GotRaceStart -= OnGotRaceStart;
    }
    

    public override void Refresh()
    {
        base.Refresh();
        
        PlayerNameLabel.Text = Game.Player.name;
        PlayerLevelLabel.Text = Game.Player.level.ToString();
        
        TracksList.QueueFreeChildren();

        foreach (TrackInfo trackInfo in Game.LatestTracks)
        {
            TracksListItem listItem = TracksListItemScene.Instantiate<TracksListItem>();
            TracksList.AddChild(listItem);
            listItem.SetTrackInfo(trackInfo);
        }
    }


    private async void PlayTrack(string id)
    {
        Result<Track> result = await Socket.SendPlayTrack(id);

        if (result.error is not null)
        {
            PopupsManager.GenericErrorDialog("Error loading track!", result.error);
            return;
        }
        
        Game.SetPage(HudPage.Scene, new HudData { track = result.data });
    }
    

    private void OnLogoutButtonPressed()
    {
        AuthManager.Logout();
    }

    private void OnTrackEditorButtonPressed()
    {
        Game.SetPage(TrackEditorPage.Scene);
    }

    private void OnGotRaceStart(Race race)
    {
        Game.SetPage(HudPage.Scene);
        Stage.StartRace(race);
    }
}