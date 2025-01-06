using Godot;

namespace TD.TileEffects;

public class TileEffectBoostLeft : TileEffect
{
	public override void Trigger(TriggerData data)
	{
		data.character.Boost += Vector2.Left * 80;
	}
}