using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TD.Models;

namespace TD.Connection;

public abstract partial class Socket
{
	private static readonly Dictionary<string, Track> PlayerTracksCache = new();


	public static async Task<Result> SendGetClientData()
	{
		return await SendExpectingError("client_data");
	}
    
	
	public static async Task<Result<Track>> SendTrackCreate(Track track)
	{
		Result<Track> result = await SendExpectingResultOrError<Track>("Track_Create", track);
		
		if (result.error is null)
		{
			ClientData.player.trackInfos.Add(new TrackInfo
			{
				id = result.data.id,
				name = result.data.name,
				plays = result.data.plays,
				createdAt = result.data.createdAt
			});
			
			PlayerTracksCache[result.data.id] = result.data;
		}
		
		return result;
	}
	
	public static async Task<Result<Track>> SendTrackUpdate(Track track)
	{
		Result<Track> result = await SendExpectingResultOrError<Track>("Track_Update", track);

		if (result.error is null)
		{
			TrackInfo trackInfo = ClientData.player.trackInfos.Find(info => info.id == result.data.id);
			
			if (trackInfo is not null)
				trackInfo.name = result.data.name;
			
			PlayerTracksCache[result.data.id] = result.data;
		}
		
		return result;
	}
	
	public static async Task<Result> SendTrackDelete(string id)
	{
		// Optimistic update
		ClientData.player.trackInfos = ClientData.player.trackInfos.Where(info => info.id != id).ToList();
		PlayerTracksCache.Remove(id);
		
		Result result = await SendExpectingError("Track_Delete", id);
		
		return result;
	}

	public static async Task<Result<Track>> SendGetTrack(string id)
	{
		if (PlayerTracksCache.TryGetValue(id, out Track track))
			return new Result<Track> { data = track };
		
		Result<Track> result = await SendExpectingResultOrError<Track>("Track", id);

		if (result.error is null)
			PlayerTracksCache[id] = result.data;
		
		return result;
	}

	public static async Task<Result<Track>> SendRaceSolo(string id)
	{
		Result<Track> result = await SendExpectingResultOrError<Track>("Race_Solo", id);

		if (result.error is null)
		{
			TrackInfo trackInfo = ClientData.player.trackInfos.Find(info => info.id == result.data.id);
			
			if (trackInfo is not null)
				trackInfo.plays += 1;
		}
        
		return result;
	}

	public static async Task<Result> SendRaceQueueEnter(string id)
	{
		return await SendExpectingError("Race_Queue_Enter", id);
	}

	public static async Task SendRaceQueueLeave()
	{
		await SendAndForget("Race_Queue_Leave");
	}

	public static async Task SendRaceQueueReady()
	{
		await SendAndForget("Race_Queue_Ready");
	}

	public static async Task SendRaceCharacterUpdate(
		Vector2 position,
		Vector2 velocity,
		int horizontalInput,
		int verticalInput
	)
	{
		RaceCharacterUpdate data = new()
		{
			x = position.X,
			y = position.Y,
			vx = velocity.X,
			vy = velocity.Y,
			
			ih = horizontalInput,
			iv = verticalInput,
		};
		
		await SendAndForget("Race_Character_Update", data);
	}

	public static async Task SendRaceCharacterFinish()
	{
		await SendAndForget("Race_Character_Finish");
	}

	public static async Task SendRaceCharacterQuit()
	{
		await SendAndForget("Race_Character_Quit");
	}
}