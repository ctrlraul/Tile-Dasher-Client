using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TD.Extensions;
using HttpClient = System.Net.Http.HttpClient;

namespace TD.Connection;

public class SseConnection
{
	private const uint ConnectionAttemptCooldownMs = 5000;
	
	public event Action Connected;
	public event Action Disconnected;
	public event Action<string> GotData;
	public event Action<string> Error;
	public event Action<int> ReconnectQueued;
	public event Action ServerClosedConnection;
	
	private readonly HttpClient Client;
	private readonly string Endpoint;
	private CancellationTokenSource disconnectionTokenSource;
	private Task disconnectionTask;
	private readonly int ReconnectDelayMsOverride;


	public SseConnection(HttpClient client, string endpoint, int reconnectDelayMsOverride)
	{
		Client = client;
		Endpoint = endpoint;
		ReconnectDelayMsOverride = reconnectDelayMsOverride;
	}
    
	
	/// <summary>
	/// Will hang for as long as it can maintain (or try to maintain) a connection.
	/// </summary>
	public async Task Connect()
	{
		if (disconnectionTokenSource != null)
			throw new Exception("Already trying to maintain a connection");
        
		int attempts = 0;
		
		disconnectionTokenSource = new CancellationTokenSource();
		disconnectionTask = disconnectionTokenSource.Token.AsTask();
		
		while (true)
		{
			attempts++;
			
			bool reconnect = await TryToConnectToSse();
			
			Disconnected?.Invoke();
			
			if (!reconnect)
				break;

			int reconnectDelayMs;
			
			if (ReconnectDelayMsOverride > 0)
				reconnectDelayMs = ReconnectDelayMsOverride;
			else
				reconnectDelayMs = (int)(ConnectionAttemptCooldownMs * Math.Max(1, attempts - 2));
			
			ReconnectQueued?.Invoke(reconnectDelayMs);
			
			await Task.Delay(reconnectDelayMs);
		}

		bool cancelled = disconnectionTokenSource.IsCancellationRequested;
		
		disconnectionTokenSource = null;
		disconnectionTask = null;
		
		if (!cancelled)
			ServerClosedConnection?.Invoke();
		// else
		// 	ClientClosedConnection?.Invoke();
	}

	public void Disconnect()
	{
		disconnectionTokenSource.Cancel();
	}

	/// <returns>bool - Whether it should try to reconnect</returns>
	private async Task<bool> TryToConnectToSse()
	{
		StreamReader reader = null;
		Stream stream = null;
		bool reconnect = true;
		
		try
		{
			HttpRequestMessage request = new(HttpMethod.Get, Endpoint);
			request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
			
			HttpResponseMessage response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, disconnectionTokenSource.Token);

			if (!response.IsSuccessStatusCode)
				throw new Exception($"Status code: {response.StatusCode}");

			stream = await response.Content.ReadAsStreamAsync(disconnectionTokenSource.Token);
			reader = new StreamReader(stream);

			Connected?.Invoke();
			
			while (true)
			{
				Task<string> readTask = reader.ReadLineAsync();
				Task firstTask = await Task.WhenAny(readTask, disconnectionTask);
				
				if (firstTask == disconnectionTask)
					throw new OperationCanceledException();
				
				string line = await readTask;

				if (line == null) // Server closed connection
				{
					reconnect = false;
					break;
				}
				
				if (line.StartsWith("data:"))
				{
					string data = line.Substring(5).Trim();
					GotData?.Invoke(data);
				}
			}
		}
		catch (OperationCanceledException)
		{
			reconnect = false;
		}
		catch (Exception exception)
		{
			Error?.Invoke(exception.Message);
		}
		
		stream?.Dispose();
		reader?.Dispose();

		return reconnect;
	}
}