using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using TD.Lib;
using TD.Models;

// Talk to Raul before touching the private Send methods

namespace TD.Connection;

public abstract partial class Socket
{
	private static uint ExchangesSent;

	private class SocketMessageException : Exception
	{
		public SocketMessageException(string message) : base(message)
		{
		}
	}

	[JsonObject]
	private class ClientMessage
	{
		public string eventName;
		public string data;
		public string exchangeId;
	}

	[JsonObject]
	private class ServerMessage
	{
		public string eventName;
		public string data;
		public string error;
		public string exchangeId;
	}

	public static class DisconnectionReason
	{
		public const string Error = "Error";
		public const string Kick = "Kick";
	}


	public static event Action Connected;
	public static event Action Disconnected;

	private const string TokenName = "x-ticket";
	private static string ServerUrl;
	private static readonly Logger Logger = new(nameof(Socket));
	private static readonly Dictionary<string, TaskCompletionSource<string>> ExchangeTasks = new();
	private static ClientWebSocket Client { get; set; }

	private static CancellationTokenSource ListeningStopper { get; set; }
	public static string LastDisconnectionReason { get; private set; }
	private static Exception lastSocketException;

	public static ClientData ClientData { get; private set; }


	private static readonly HashSet<string> NoLogList = [
		"Race_Character_Update"
	];


	#region Api
	
	public static async Task Connect(string serverUrl)
	{
		ServerUrl = serverUrl;
        
		if (Client?.State is WebSocketState.Open or WebSocketState.Connecting)
		{
			Logger.Log("Already connected or connecting");
			return;
		}

		LastDisconnectionReason = null;

		if (ServerUrl.Contains("localhost"))
			await Task.Delay(500); // Simulate latency

		Client = new ClientWebSocket();
		Client.Options.SetRequestHeader(TokenName, TokenManager.Token);

		try
		{
			await Client.ConnectAsync(new Uri(ServerUrl), default);
		}
		catch (Exception exception)
		{
			Logger.Log("Failed to connect to socket!");
			Logger.Log(exception);
			throw;
		}

		ListeningStopper = new CancellationTokenSource();

		Task stopTask = Task.WhenAny(Listen(), MonitorConnection());
		_ = stopTask.ContinueWith(_ => Callable.From(OnStop).CallDeferred());
		
		Connected?.Invoke();
	}
	
	public static void Disconnect(string reason)
	{
		LastDisconnectionReason = reason;
		ListeningStopper?.Cancel();
	}
	
	#endregion
	

	private static async Task Listen()
	{
		while (Client.State == WebSocketState.Open)
		{
			try
			{
				byte[] buffer = new byte[32768];
				WebSocketReceiveResult result = await Client.ReceiveAsync(buffer, ListeningStopper.Token);

				if (ListeningStopper.IsCancellationRequested) // Disconnect called
					break;
				
				if (result.MessageType == WebSocketMessageType.Close)
				{
					LastDisconnectionReason = DisconnectionReason.Kick;
					break;
				}

				string messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
				ServerMessage serverMessage = JsonConvert.DeserializeObject<ServerMessage>(messageJson);
				GotMessage(serverMessage);
			}
			catch (TaskCanceledException)
			{
				break;
			}
			catch (Exception exception)
			{
				LastDisconnectionReason = DisconnectionReason.Error;
				lastSocketException = exception;
				break;
			}
		}
	}

	private static async Task MonitorConnection()
	{
		while (Client.State == WebSocketState.Open)
		{
			await Task.Delay(1000);

			if (Client.State == WebSocketState.Open)
				continue;

			LastDisconnectionReason ??= DisconnectionReason.Error;

			break;
		}
	}


	private static async Task InternalSend(string eventName, object data, string exchangeId = null)
	{
		if (!NoLogList.Contains(eventName))
			Logger.Log($"[SENT] {eventName}{(exchangeId is null ? "" : " #" + exchangeId)}");

		ClientMessage clientMessage = new()
		{
			exchangeId = exchangeId,
			eventName = eventName,
			data = JsonConvert.SerializeObject(data)
		};

		string json = JsonConvert.SerializeObject(clientMessage);
		byte[] buffer = Encoding.UTF8.GetBytes(json);

		// Let the other methods catch it, so they can pass the exception to ExchangeTasks
		await Client.SendAsync(buffer, WebSocketMessageType.Text, true, default);
	}

	private static async Task<string> InternalSendExchange(string eventName, object data)
	{
		string exchangeId = (++ExchangesSent).ToString();
		TaskCompletionSource<string> tcs = new();
		ExchangeTasks[exchangeId] = tcs;

		_ = tcs.Task.ContinueWith(task =>
		{
			ExchangeTasks.Remove(exchangeId);

			if (!task.IsFaulted)
				return;

			Exception exception = task.Exception?.InnerException ?? task.Exception;
			string message = exception is SocketMessageException ? " " + exception.Message : "\n" + exception;
			Logger.Log($"Error for exchange '{eventName}-{exchangeId}':{message}");
		});

		try
		{
			await InternalSend(eventName, data, exchangeId);
		}
		catch (Exception exception)
		{
			tcs.SetException(exception);
		}

		return await tcs.Task;
	}

	private static async Task<Result<T>> SendExpectingResultOrError<T>(string eventName, object data = null)
	{
		Result<T> result = new();

		try
		{
			var json = await InternalSendExchange(eventName, data);
			result.data = JsonConvert.DeserializeObject<T>(json);
		}
		catch (Exception exception)
		{
			result.error = exception.Message;
		}

		return result;
	}

	private static async Task<Result> SendExpectingError(string eventName, object data = null)
	{
		Result result = new();

		try
		{
			await InternalSendExchange(eventName, data);
		}
		catch (Exception exception)
		{
			result.error = exception.Message;
		}

		return result;
	}

	private static async Task SendAndForget(string eventName, object data = null)
	{
		try
		{
			await InternalSend(eventName, data);
		}
		catch (Exception exception)
		{
			Logger.Log($"Error for message '{eventName}': {exception}");
		}
	}
	

	private static async void OnStop()
	{
		if (lastSocketException is not null)
			Logger.Log($"Error:\n{lastSocketException}");

		if (Client.State == WebSocketState.Open)
			await Client.CloseAsync(WebSocketCloseStatus.NormalClosure, LastDisconnectionReason, CancellationToken.None);

		lastSocketException = null;
		ClientData = null;

		ListeningStopper?.Dispose();
		ListeningStopper = null;

		Client?.Dispose();
		Client = null;

		// bool shouldTryToReconnect = LastDisconnectionReason is null or DisconnectionReason.Error;
		Disconnected?.Invoke();
	}
}