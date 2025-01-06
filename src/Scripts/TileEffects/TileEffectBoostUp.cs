namespace TD.TileEffects;

public class TileEffectBoostUp : TileEffect
{
	public override void Trigger(TriggerData data)
	{
		data.character.Velocity = data.character.Velocity with { Y = -1600 };
	}
}