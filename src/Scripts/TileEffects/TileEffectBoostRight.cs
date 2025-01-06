using Godot;

namespace TD.TileEffects;

public class TileEffectBoostRight : TileEffect
{
	public override void Trigger(TriggerData data)
	{
		data.character.Boost += Vector2.Right * 80;
	}
}