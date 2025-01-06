namespace TD.TileEffects;

public class TileEffectFinish : TileEffect
{
	public override void Trigger(TriggerData data)
	{
		Stage.Finish(data.character);
	}
}