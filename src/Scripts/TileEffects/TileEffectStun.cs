namespace TD.TileEffects;

public class TileEffectStun : TileEffect
{
	public override void Trigger(TriggerData data)
	{
		data.character.Stun();
	}
}