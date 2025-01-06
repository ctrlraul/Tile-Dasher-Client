using Godot;

namespace TD.TileEffects;

public class TileEffectBoostDown : TileEffect
{
	public override void Trigger(TriggerData data)
	{
		data.character.Boost += Vector2.Down * 80;
	}
}