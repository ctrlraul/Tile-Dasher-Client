using System;
using System.Net.Http;
using System.Threading.Tasks;
using TD.Lib;

namespace TD.Connection;

public abstract class AuthManager
{
	private static string ServerUrl;
	private const string TicketHeaderName = "x-ticket";

	private static readonly Logger Logger = new("Server");
	private static HttpClient Client;

	public static event Action LoggedIn;
	public static event Action LoggedOut;

	public static bool IsLoggedIn { get; private set; }
    

	public static void Initialize(string serverUrl)
	{
		ServerUrl = serverUrl;

		Client?.Dispose();
		Client = new HttpClient { BaseAddress = new Uri(ServerUrl) };

		if (TokenManager.Token != null)
			Client.DefaultRequestHeaders.Add(TicketHeaderName, TokenManager.Token);
	}


	#region Login Methods
    
	public static async void LoginWithGoogle()
	{
		if (IsLoggedIn)
			throw new Exception("Already logged in?");
        
		Logger.Log("Signing in with Google");
		
		try
		{
			string ticketId = await Oauth.Open(
				ServerUrl + "auth/google",
				ServerUrl + "auth/success",
				TicketHeaderName
			);
			
			if (ticketId == null)
				throw new Exception("Got no ticket");
			
			// Logger.Log("Ticket: " + ticketId);
			
			SetTicket(ticketId);
			
			LoggedIn?.Invoke();
		}
		catch (Exception exception)
		{
			Logger.Log($"Error logging in with Google: {exception.Message}");
		}
	}

	public static async void LoginAsGuest()
	{
		Logger.Log("Login as guest!");
	}
	
	public static async Task LoginAsPlayer(string playerId)
	{
		TokenManager.UseTokenStorage = false;
		
		Result<string> result = await Cherry("auth/impersonate").Query("id", playerId).Get<string>();

		if (result.error != null)
			throw new Exception(result.error);
		
		SetTicket(result.data);
	}

	#endregion
    
	
	public static void Logout()
	{
		IsLoggedIn = false;
		
		Task _ = Client.GetAsync("auth/clear").ContinueWith(task =>
		{
			if (task.Exception is not null)
				Logger.Log($"Exception logging out: {task.Exception.Message}");
		});
		
		SetTicket(null);
		
		LoggedOut?.Invoke();
	}

	public static async Task UpdateLoggedInState()
	{
		if (TokenManager.Token == null)
		{
			IsLoggedIn = false;
		}
		else
		{
			HttpResponseMessage response = await Client.GetAsync("auth/state");
		
			string content = await response.Content.ReadAsStringAsync();

			if (content == "1")
			{
				IsLoggedIn = true;
			}
			else
			{
				// Clear ticket since the server doesn't honor it
				SetTicket(null);
			}
		}
	}

	
	private static void SetTicket(string ticketId)
	{
		TokenManager.Token = ticketId;
		
		Client.DefaultRequestHeaders.Remove(TicketHeaderName);

		IsLoggedIn = ticketId != null;
		
		if (IsLoggedIn)
			Client.DefaultRequestHeaders.Add(TicketHeaderName, ticketId);
	}
	
	private static Cherry Cherry(string endpoint)
	{
		return new Cherry(Client, endpoint);
	}
}