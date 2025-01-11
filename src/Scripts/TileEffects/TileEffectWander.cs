using System;
using Godot;
using TD.Entities;

namespace TD.TileEffects;

public class TileEffectWander : TileEffect
{
	public override bool PopOff { get; } = true;
	
	public override void Trigger(TriggerData data)
	{
		OngoingWanderEffect effect = new OngoingWanderEffect();
		Stage.Temp.AddChild(effect);
		effect.Init(data.coord);
	}
}

public partial class OngoingWanderEffect : Node
{
	private static readonly Vector2I[] Directions =
	{
		Vector2I.Up,
		Vector2I.Down,
		Vector2I.Left,
		Vector2I.Right
	};
    
	private Vector2I Coord;
	private ulong StartFrame;

	public override void _PhysicsProcess(double delta)
	{
		ulong interval = (ulong)Engine.PhysicsTicksPerSecond * 2;
		ulong current = StartFrame + Engine.GetPhysicsFrames();

		if (current % interval != 0)
			return;
		
		Trigger();
	}

	public void Init(Vector2I coord)
	{
		Coord = coord;
		StartFrame = Engine.GetPhysicsFrames();
	}

	private void Trigger()
	{
		Vector2I direction = Directions[Random.Shared.Next() % Directions.Length];
		Vector2I newCoord = Coord + direction;

		if (Stage.TileGrid.GetTile(newCoord) is not null)
			return;

		foreach (Character character in Stage.GetCharacters())
		{
			if (character.Coord == newCoord)
				return;
		}
		
		Stage.TileGrid.MoveTile(Coord, newCoord);

		Coord = newCoord;
	}
}