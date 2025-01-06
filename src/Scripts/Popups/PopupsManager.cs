using System.Threading.Tasks;
using Godot;
using TD.Exceptions;

namespace TD.Popups;

public partial class PopupsManager : Node
{
    public static PopupsManager Instance { get; private set; }

    public bool HasPopups => GetChildCount() > 0;
    
    private static readonly PackedScene DialogPopupScene = GD.Load<PackedScene>("res://Scenes/Popups/DialogPopup.tscn");
    private static readonly PackedScene SelectTrackPopupScene = GD.Load<PackedScene>("res://Scenes/Popups/TracksPopup.tscn");


    public override void _Ready()
    {
        base._Ready();

        if (Instance != null)
            throw new SingletonException(GetType());
        
        Instance = this;
    }


    public static DialogPopup Dialog()
    {
        DialogPopup popup = DialogPopupScene.Instantiate<DialogPopup>();
        Instance.AddChild(popup);
        return popup;
    }

    public static DialogPopup GenericErrorDialog(string title, string message)
    {
        DialogPopup popup = Dialog();
        
        popup.SetTitle(title)
            .SetMessage(message)
            .SetStyle(DialogPopup.Style.Error)
            .AddButton("Ok")
            .SetCancellable(true);

        return popup;
    }
    
    public static async Task PleaseWait(Task task, string title = "Please wait...")
    {
        GenericPopup popup = Dialog()
            .SetTitle(title)
            .SetSpinner(true)
            .SetCancellable(false);

        await task;
        
        popup.Remove();
    }

    public static TracksPopup SelectTrack()
    {
        TracksPopup popup = SelectTrackPopupScene.Instantiate<TracksPopup>();
        Instance.AddChild(popup);
        return popup;
    }
}