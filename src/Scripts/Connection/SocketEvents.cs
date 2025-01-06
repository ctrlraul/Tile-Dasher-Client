using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TD.Models;

namespace TD.Connection;

public abstract partial class Socket
{
	public static event Action<ClientData> GotClientData;
	public static event Action GotClientDataError;
	
	
	private static void GotMessage(ServerMessage message)
	{
		Logger.Log($"* <-- {message.eventName}{(message.exchangeId is null ? "" : " #" + message.exchangeId)}");

		
		bool isExchange = message.exchangeId is not null;

		
		switch (message.eventName)
		{
			case "Client_Data":
				ClientData = JsonConvert.DeserializeObject<ClientData>(message.data);
				GotClientData?.Invoke(ClientData);
				break;
			
			case "Client_Data_Error":
				GotClientDataError?.Invoke();
				break;

			default:
				if (!isExchange)
				{
					string json = JsonConvert.SerializeObject(message, Formatting.Indented);
					Logger.Log($"Unhandled server message:\n'{json}'");
				}
				break;
		}


		if (isExchange)
		{
			TaskCompletionSource<string> tcs = ExchangeTasks[message.exchangeId!];
		
			if (message.error is null)
				tcs.SetResult(message.data);
			else
				tcs.SetException(new SocketMessageException(message.error));
		}
	}
}