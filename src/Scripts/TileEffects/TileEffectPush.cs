using Godot;
using TD.Enums;

namespace TD.TileEffects;

public class TileEffectPush : TileEffect
{
	public override bool PopOff { get; } = true;
	
	public override void Trigger(TriggerData data)
	{
		Vector2I direction = data.trigger switch
		{
			TileEffectTrigger.Bump => Vector2I.Up,
			TileEffectTrigger.Stand => Vector2I.Down,
			TileEffectTrigger.PushLeft => Vector2I.Left,
			_ => Vector2I.Right
		};

		Vector2I newCoord = data.coord + direction;

		if (Stage.TileGrid.GetTile(newCoord) is not null)
			return;

		foreach (Character character in Stage.GetCharacters())
		{
			if (character.Coord == newCoord)
				return;
		}
		
		Stage.TileGrid.MoveTile(data.coord, newCoord);
	}
}