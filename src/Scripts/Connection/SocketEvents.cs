using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TD.Models;

namespace TD.Connection;

public abstract partial class Socket
{
	public static event Action<ClientData> GotClientData;
	public static event Action GotClientDataError;
	public static event Action<string> GotRacesQueueUpdate;
	public static event Action<Race> GotRaceStart;
	public static event Action<RaceCharacterUpdate> GotRaceCharacterUpdate;
	public static event Action<RaceCharacterFinish> GotRaceCharacterFinished;
    
	
	private static void GotMessage(ServerMessage message)
	{
		if (!NoLogList.Contains(message.eventName))
			Logger.Log($"[GOT] {message.eventName}{(message.exchangeId is null ? "" : " #" + message.exchangeId)}");

		
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

			case "Race_Queue_Enter":
			{
				RaceQueueEnter rqEnter = JsonConvert.DeserializeObject<RaceQueueEnter>(message.data);

				if (!ClientData.racesQueue.TryGetValue(rqEnter.trackId, out RacesQueueEntry rqEntry))
				{
					rqEntry = new RacesQueueEntry { trackId = rqEnter.trackId };
					ClientData.racesQueue.Add(rqEnter.trackId, rqEntry);
				}

				rqEntry.players.Add(rqEnter.player.id, rqEnter.player);
				
				GotRacesQueueUpdate?.Invoke(rqEntry.trackId);
				break;
			}

			case "Race_Queue_Leave":
			{
				RaceQueueLeave rqLeave = JsonConvert.DeserializeObject<RaceQueueLeave>(message.data);
				RacesQueueEntry rqEntry = ClientData.racesQueue[rqLeave.trackId];

				rqEntry.playersReady = rqEntry.playersReady.Where(id => id != rqLeave.playerId).ToList();
				rqEntry.players.Remove(rqLeave.playerId);
				
				if (rqEntry.players.Count == 0)
					ClientData.racesQueue.Remove(rqLeave.trackId);
				
				GotRacesQueueUpdate?.Invoke(rqEntry.trackId);
				break;
			}

			case "Race_Queue_Ready":
			{
				RaceQueueReady rqReady = JsonConvert.DeserializeObject<RaceQueueReady>(message.data);
				RacesQueueEntry rqEntry = ClientData.racesQueue[rqReady.trackId];
				rqEntry.playersReady.Add(rqReady.playerId);
				GotRacesQueueUpdate?.Invoke(rqEntry.trackId);
				break;
			}

			case "Race_Queue_Clear":
			{
				string trackId = JsonConvert.DeserializeObject<string>(message.data);
				ClientData.racesQueue.Remove(trackId);
				GotRacesQueueUpdate?.Invoke(trackId);
				break;
			}

			case "Race_Start":
			{
				Race race = JsonConvert.DeserializeObject<Race>(message.data);
				GotRaceStart?.Invoke(race);
				break;
			}

			case "Race_Character_Update":
			{
				RaceCharacterUpdate update = JsonConvert.DeserializeObject<RaceCharacterUpdate>(message.data);
				GotRaceCharacterUpdate?.Invoke(update);
				break;
			}

			case "Race_Character_Finish":
			{
				RaceCharacterFinish finish = JsonConvert.DeserializeObject<RaceCharacterFinish>(message.data);
				GotRaceCharacterFinished?.Invoke(finish);
				break;
			}

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