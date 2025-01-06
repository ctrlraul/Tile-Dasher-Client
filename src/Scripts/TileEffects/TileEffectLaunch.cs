using Godot;
using TD.Enums;

namespace TD.TileEffects;

public class TileEffectLaunch : TileEffect
{
	private const float Force = 4000;
	
	public override void Trigger(TriggerData data)
	{
		Vector2 impulse = Game.GridToWorld(data.coord).DirectionTo(data.character.Position) * Force;
		
		switch (data.trigger)
		{
			case TileEffectTrigger.Bump:
			case TileEffectTrigger.Stand:
				data.character.Velocity = impulse + new Vector2(data.character.Velocity.X, 0);
				break;
			
			case TileEffectTrigger.PushLeft:
			case TileEffectTrigger.PushRight:
				data.character.Velocity = impulse + new Vector2(0, data.character.Velocity.Y);
				break;
		}
	}
}