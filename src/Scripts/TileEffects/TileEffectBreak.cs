namespace TD.TileEffects;

public class TileEffectBreak : TileEffect
{
	public override bool PopOff { get; } = true;

	public override void Trigger(TriggerData data)
	{
		Stage.AddCrumbleParticles(data.coord);
		Stage.AddCrumbleParticles(data.coord);
		Stage.AddCrumbleParticles(data.coord);
		Stage.TileGrid.RemoveTile(data.coord);
	}
}