using Godot;
using TD.Connection;

namespace TD.Pages.Welcome;

public partial class WelcomePage : Page
{
	public static readonly PackedScene Scene = GD.Load<PackedScene>("res://Scenes/Pages/Welcome/WelcomePage.tscn");

	private void OnLoginWithGoogleButtonPressed()
	{
		Server.LoginWithGoogle();
	}

	private void OnLoginAsGuestButtonPressed()
	{
		Server.LoginAsGuest();
	}
}