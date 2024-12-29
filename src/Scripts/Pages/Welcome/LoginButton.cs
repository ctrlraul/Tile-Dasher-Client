using Godot;

namespace MechGame.Pages.Welcome;

[Tool]
public partial class LoginButton : Control
{
    [Signal] public delegate void PressedEventHandler();
    
    
    private Texture2D _providerLogo;
    private string _providerName;
    
    [Export]
    public Texture2D ProviderLogo
    {
        get => _providerLogo;
        set
        {
            _providerLogo = value;
            if (logoTextureRect is not null)
                logoTextureRect.Texture = _providerLogo;
        }
    }
    
    [Export]
    public string ProviderName
    {
        get => _providerName;
        set
        {
            _providerName = value;
            if (nameLabel is not null)
                nameLabel.Text = _providerName;
        }
    }
    

    private TextureRect logoTextureRect;
    private Label nameLabel;
    private Button button;

    
    public override void _Ready()
    {
        base._Ready();
        
        logoTextureRect = GetNode<TextureRect>("%LogoTextureRect");
        nameLabel = GetNode<Label>("%NameLabel");
        button = GetNode<Button>("%Button");
        
        ProviderLogo = _providerLogo;
        ProviderName = _providerName;
        button.Pressed += OnButtonPressed;
    }

    private void OnButtonPressed()
    {
        EmitSignal(SignalName.Pressed);
    }
}