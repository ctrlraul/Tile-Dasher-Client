namespace TD.TileEffects;

public class TileEffectRespawn : TileEffect
{
	public override void Trigger(TriggerData data)
	{
		data.character.Respawn();
	}
}