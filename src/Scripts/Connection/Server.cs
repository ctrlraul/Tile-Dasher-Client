using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TD.Models;
using TD.Lib;
using HttpClient = System.Net.Http.HttpClient;

namespace TD.Connection;

public class Result
{
	public string error;
}

public class Result<T> : Result
{
	public T data;
}

public abstract partial class Server
{
	private static string ServerUrl;
	private const string TicketHeaderName = "x-ticket";

	private static readonly Logger Logger = new("Server");
	private static HttpClient Client;
	private static SseConnection sseConnection;

	public static event Action LoggedIn;
	public static event Action LoggedOut;
	public static event Action Connected;
	public static event Action Disconnected;
	public static event Action SseInitialData;

	private static InitialData initialData;
	public static uint DisconnectionsSinceLogin { get; private set; }
	public static bool IsLoggedIn { get; private set; }
	
	public static List<Tile> Tiles => initialData?.tiles ?? new List<Tile>();
	public static Player Player => initialData?.player ?? Player.Dummy;


	static Server()
	{
		LoggedIn += () => DisconnectionsSinceLogin = 0;
	}
    

	public static void Initialize(string serverUrl)
	{
		ServerUrl = serverUrl;
        
		if (Client != null)
			throw new Exception("Already initialized");
		
		Client = new HttpClient { BaseAddress = new Uri(ServerUrl) };

		if (TokenManager.Token != null)
			Client.DefaultRequestHeaders.Add(TicketHeaderName, TokenManager.Token);
	}
	
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
		
		Result<string> result = await Send<string>("auth/impersonate", playerId);

		if (result.error != null)
			throw new Exception(result.error);
		
		SetTicket(result.data);
	}
    
	public static void Logout()
	{
		IsLoggedIn = false;
        
		sseConnection.Disconnect();
		
		Task _ = Client.GetAsync("auth/clear").ContinueWith(task =>
		{
			if (task.Exception != null)
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
	
	public static async Task Connect(int reconnectDelayMsOverride)
	{
		Logger.Log("Connecting to SSE...");
		
		sseConnection = new SseConnection(Client, "sse", reconnectDelayMsOverride);
		sseConnection.Connected += OnSseConnected;
		sseConnection.Disconnected += OnSseDisconnected;
		sseConnection.GotData += OnSseConnectionGotData;
		sseConnection.ServerClosedConnection += OnSseConnectionServerClosedConnection;
		sseConnection.Error += message => Logger.Log($"Error on SSE connection: {message}");
		sseConnection.ReconnectQueued += delayMs => Logger.Log($"Trying to reconnect in {delayMs / 1000} seconds...");
		
		await sseConnection.Connect();
	}

	
	
	private static async Task<Result> Get(string path, object data = null)
	{
		Logger.Log($"* --> GET [{path}]");
		
		Result result = new();

		try
		{
			HttpResponseMessage response = await Client.GetAsync(path);
			
			string responseContent = await response.Content.ReadAsStringAsync();
			
			if (!response.IsSuccessStatusCode)
				throw new Exception($"{response.ReasonPhrase}: {responseContent}");
		}
		catch (Exception exception)
		{
			Logger.Log($"Error sending [{path}]: {exception.Message}");
			result.error = exception.Message;
		}

		return result;
	}
	
    

	private static async Task<Result> Send(string path, object data = null)
	{
		string method = data == null ? "GET" : "POST";
		
		Logger.Log($"* --> {method} [{path}]");
		
		Result result = new();

		try
		{
			HttpResponseMessage response;
			
			if (method == "POST")
			{
				string json = JsonConvert.SerializeObject(data);
				StringContent content = new(json, Encoding.UTF8, "application/json");
				response = await Client.PostAsync(path, content);
			}
			else
			{
				response = await Client.GetAsync(path);
			}
			
			string responseContent = await response.Content.ReadAsStringAsync();
			
			if (!response.IsSuccessStatusCode)
				throw new Exception($"{response.ReasonPhrase}: {responseContent}");
		}
		catch (Exception exception)
		{
			Logger.Log($"Error sending [{path}]: {exception.Message}");
			result.error = exception.Message;
		}

		return result;
	}

	private static async Task<Result<T>> Send<T>(string path, object data = null)
	{
		string method = data == null ? "GET" : "POST";
		
		Logger.Log($"* --> {method} [{path}]");
		
		Result<T> result = new();

		try
		{
			HttpResponseMessage response;
			
			if (method == "POST")
			{
				string json = JsonConvert.SerializeObject(data);
				StringContent content = new(json, Encoding.UTF8, "application/json");
				response = await Client.PostAsync(path, content);
			}
			else
			{
				response = await Client.GetAsync(path);
			}
			
			string responseContent = await response.Content.ReadAsStringAsync();
			
			if (!response.IsSuccessStatusCode)
				throw new Exception($"{response.ReasonPhrase}: {responseContent}");

			result.data = JsonConvert.DeserializeObject<T>(responseContent);
		}
		catch (Exception exception)
		{
			Logger.Log($"Error sending [{path}]: {exception.Message}");
			result.error = exception.Message;
		}

		return result;
	}

	private static void SetTicket(string ticketId)
	{
		TokenManager.Token = ticketId;
		
		Client.DefaultRequestHeaders.Remove(TicketHeaderName);

		IsLoggedIn = ticketId != null;
		
		if (IsLoggedIn)
			Client.DefaultRequestHeaders.Add(TicketHeaderName, ticketId);
	}
	
	
	private static void OnSseConnected()
	{
		Logger.Log("Connected to SSE");
		Connected?.Invoke();
	}

	private static void OnSseDisconnected()
	{
		DisconnectionsSinceLogin++;
		Disconnected?.Invoke();
	}
	
	private static void OnSseConnectionServerClosedConnection()
	{
		Logger.Log("Stream ended by server.");
	}
	
	private static void OnSseConnectionGotData(string sseJson)
	{
		ServerSentEvent sse = JsonConvert.DeserializeObject<ServerSentEvent>(sseJson);
		
		Logger.Log($"* <-- SSE [{sse.name}]");

		switch (sse.name)
		{
			case "initial_data":
				initialData = JsonConvert.DeserializeObject<InitialData>(sse.json);
				SseInitialData?.Invoke();
				break;
			
			default:
				Logger.Log($"Unhandled SSE '{sse.name}': {sse.json}");
				break;
		}
	}
}