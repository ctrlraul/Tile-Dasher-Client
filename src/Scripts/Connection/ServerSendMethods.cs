using System.Threading.Tasks;
using TD.Models;

namespace TD.Connection;

public abstract partial class Server
{
	private static Cherry Cherry(string endpoint)
	{
		return new Cherry(Client, endpoint);
	}
	
	public static async Task<Result<Track>> SendTrackCreate(Track track)
	{
		return await Cherry("/track/create").Post<Track>(track);
	}
	
	public static async Task<Result<Track>> SendTrackUpdate(Track track)
	{
		return await Cherry("/track/update").Post<Track>(track);
	}

	public static async Task<Result<Track>> SendGetTrack(string id)
	{
		return await Cherry("/track").Query("id", id).Get<Track>();
	}

	public static async Task<Result<Track>> SendPlayTrack(string id)
	{
		Result<Track> result = await Cherry("/track/play").Query("id", id).Get<Track>();

		if (result.error is null)
		{
			TrackInfo trackInfo = Player.trackInfos.Find(info => info.id == result.data.id);
			
			if (trackInfo is not null)
				trackInfo.plays += 1;
		}
        
		return result;
	}
}