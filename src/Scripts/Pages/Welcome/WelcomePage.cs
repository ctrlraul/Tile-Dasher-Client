using Godot;
using TD.Connection;

namespace TD.Pages.Welcome;

public partial class WelcomePage : Page
{
	public static readonly PackedScene Scene = GD.Load<PackedScene>("res://Scenes/Pages/Welcome/WelcomePage.tscn");


	private void OnLoginWithGoogleButtonPressed()
	{
		AuthManager.LoginWith(AuthProvider.Google);
	}
	
	private void OnLoginWithDiscordButtonPressed()
	{
		AuthManager.LoginWith(AuthProvider.Discord);
	}

	private void OnLoginAsGuestButtonPressed()
	{
		AuthManager.LoginAsGuest();
	}
}