using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TD.Lib;

namespace TD.Connection;

public abstract class AuthProvider
{
	public const string Google = "google";
	public const string Discord = "discord";
}

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
    
	public static async void LoginWith(string provider)
	{
		if (IsLoggedIn)
			throw new Exception("Already logged in?");
        
		Logger.Log("Signing in with:", provider);
		
		try
		{
			string ticketId = await Oauth.Open(
				ServerUrl + "auth/" + provider,
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
			Logger.Log($"Error logging in with \"{provider}\": {exception.Message}");
		}
	}

	public static async void LoginAsGuest()
	{
		Result<string> result = await Cherry("auth/guest").Get<string>();
		
		if (result.error != null)
			throw new Exception(result.error);
		
		SetTicket(result.data);
		
		LoggedIn?.Invoke();
	}
	
	public static async Task LoginAsPlayer(string playerId)
	{
		TokenManager.UseTokenStorage = false;

		Dictionary<string, string> data = new() { { "playerId", playerId } };
		Result<string> result = await Cherry("auth/impersonate").Post<string>(data);

		if (result.error != null)
			throw new Exception(result.error);
		
		SetTicket(result.data);
		
		LoggedIn?.Invoke();
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