using System;
using System.Collections.Generic;
using Godot;

namespace TD.TileEffects;

public class TileEffectTeleport : TileEffect
{
	private const long CooldownMs = 1000;
	
	public override bool NeedIndexing { get; } = true;
    
	public override void Trigger(TriggerData data)
	{
		long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		if (now - data.character.LastTeleportTimeMs < CooldownMs)
			return;

		data.character.LastTeleportTimeMs = now;
		
		List<Vector2I> index = Stage.TilesIndex[data.tile.id];

		int i = index.IndexOf(data.coord);
		int next = (i + 1) % index.Count;

		Vector2 offset = data.character.Position - data.coord * Game.Config.tileSize;
		data.character.Position = index[next] * Game.Config.tileSize + offset;
		data.character.LastTeleportTimeMs = now;
	}
}