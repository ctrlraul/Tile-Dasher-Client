using System;
using Godot;
using Godot.Collections;
using TD.Enums;

namespace TD.TileEffects;

public class TileEffectCrumble : TileEffect
{
	private const float DamageScale = 0.025f;
	
	private static readonly Dictionary<Vector2I, float> Healths = new();
    
	public override bool PopOff { get; } = true;
	
	public override void Trigger(TriggerData data)
	{
		float velocity = Math.Abs(data.trigger switch
		{
			TileEffectTrigger.Bump => Math.Min(0, data.character.VelocityLastUpdate.Y),
			TileEffectTrigger.Stand => Math.Max(0, data.character.VelocityLastUpdate.Y),
			TileEffectTrigger.PushLeft => Math.Min(0, data.character.VelocityLastUpdate.X),
			TileEffectTrigger.PushRight => Math.Max(0, data.character.VelocityLastUpdate.X),
			_ => 0,
		});

		if (velocity == 0)
			return;
		
		if (!Healths.ContainsKey(data.coord))
			Healths.Add(data.coord, 100);

		int before = (int)Math.Ceiling(Healths[data.coord] / 10);

		Healths[data.coord] -= velocity * DamageScale;

		int after = (int)Math.Max(0, Math.Ceiling(Healths[data.coord] / 10));

		for (int i = before; i > after; i--)
			Stage.AddCrumbleParticles(data.coord);

		if (Healths[data.coord] <= 0)
		{
			data.character.Velocity = data.character.Velocity with { Y = data.character.VelocityLastUpdate.Y };
			Stage.AddCrumbleParticles(data.coord);
			Stage.TileGrid.RemoveTile(data.coord);
			Healths.Remove(data.coord);
		}
	}
}