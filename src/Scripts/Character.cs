using Godot;
using TD.Enums;
using TD.Models;

namespace TD;

public partial class Character : CharacterBody2D
{
	public readonly float Speed = 70;
	public readonly float JumpPower = 1300;
	public readonly float Gravity = 35;
	public readonly Vector2 Damp = new(0.85f, 0.98f);


	private Node2D Gfx;
	private CollisionShape2D NormalShape;
	private CollisionShape2D JumpingShape;
	private Label NameLabel;
	private Node2D HitCheckDownA;
	private Node2D HitCheckDownB;
	private Node2D HitCheckUpA;
	private Node2D HitCheckUpB;
	private Node2D HitCheckLeftA;
	private Node2D HitCheckLeftB;
	private Node2D HitCheckRightA;
	private Node2D HitCheckRightB;

	private ulong lastBumpFrame;
	public Vector2 RespawnPoint;
	public string PlayerName { get; private set; }


	public override void _Ready()
	{
		base._Ready();
		Gfx = GetNode<Node2D>("%Gfx");
		NormalShape = GetNode<CollisionShape2D>("%NormalShape");
		JumpingShape = GetNode<CollisionShape2D>("%JumpingShape");
		NameLabel = GetNode<Label>("%NameLabel");
		HitCheckDownA = GetNode<Node2D>("%HitChecks/DownA");
		HitCheckDownB = GetNode<Node2D>("%HitChecks/DownB");
		HitCheckUpA = GetNode<Node2D>("%HitChecks/UpA");
		HitCheckUpB = GetNode<Node2D>("%HitChecks/UpB");
		HitCheckLeftA = GetNode<Node2D>("%HitChecks/LeftA");
		HitCheckLeftB = GetNode<Node2D>("%HitChecks/LeftB");
		HitCheckRightA = GetNode<Node2D>("%HitChecks/RightA");
		HitCheckRightB = GetNode<Node2D>("%HitChecks/RightB");
		SetPlayerName(null);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		float normalizedDelta = (float)delta * Engine.PhysicsTicksPerSecond;
		Vector2 motion = new();

		motion += new Vector2(Input.GetAxis("move_left", "move_right") * Speed, 0);
		motion += Vector2.Down * Gravity;

		if (IsOnFloor() && Input.IsActionPressed("jump"))
			motion += Vector2.Up * JumpPower;

		if (motion.X != 0)
			Gfx.Scale = Gfx.Scale with { X = motion.X > 0 ? 1 : -1 };

		Velocity += motion * normalizedDelta;
		Velocity *= new Vector2(Mathf.Pow(Damp.X, normalizedDelta), Mathf.Pow(Damp.Y, normalizedDelta));
		
		MoveAndSlide();

		if (Position.Y > Stage.LowerBoundary)
			Respawn();

		HandleCollisions();

		NormalShape.Disabled = Velocity.Y < 0;
		JumpingShape.Disabled = !NormalShape.Disabled;
	}


	private void HandleCollisions()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			Vector2 normal = collision.GetNormal();

			switch (normal.X, normal.Y)
			{
				case (0, 1):
					TouchTileUp();
					break;
				
				case (0, -1):
					TouchTileDown();
					break;
				
				case (1, 0):
					TouchTileLeft();
					break;
				
				case (-1, 0):
					TouchTileRight();
					break;
			}
		}
	}
	

	private void TouchTileUp()
	{
		ulong currentFrame = Engine.GetPhysicsFrames();

		if (currentFrame - lastBumpFrame < 1)
			return;

		lastBumpFrame = currentFrame;

		var (tile, coord) = GetTileOnTop();
		
		Stage.TriggerTileEffects(TileEffectTrigger.Any, tile, coord, this);
		Stage.TriggerTileEffects(TileEffectTrigger.Bump, tile, coord, this);
	}

	private void TouchTileDown()
	{
		var (tile, coord) = GetTileBelow();

		if (tile is { safe: true })
			RespawnPoint = Stage.GridToWorld(coord);
		
		Stage.TriggerTileEffects(TileEffectTrigger.Any, tile, coord, this);
		Stage.TriggerTileEffects(TileEffectTrigger.Stand, tile, coord, this);
	}

	private void TouchTileLeft()
	{
		var (tile, coord) = GetTileToLeft();
		Stage.TriggerTileEffects(TileEffectTrigger.Any, tile, coord, this);
		Stage.TriggerTileEffects(TileEffectTrigger.PushLeft, tile, coord, this);
	}

	private void TouchTileRight()
	{
		var (tile, coord) = GetTileToRight();
		Stage.TriggerTileEffects(TileEffectTrigger.Any, tile, coord, this);
		Stage.TriggerTileEffects(TileEffectTrigger.PushRight, tile, coord, this);
	}


	public void SetPlayerName(string name)
	{
		PlayerName = name;
		NameLabel.Text = name;
		NameLabel.Visible = !string.IsNullOrEmpty(name);
	}


	private void Respawn()
	{
		Position = RespawnPoint;
		Velocity = Vector2.Zero;
	}
	

	private (Tile, Vector2I) GetTileBelow()
	{
		return GetNearestHittingTile(HitCheckDownA.GlobalPosition, HitCheckDownB.GlobalPosition);
	}

	private (Tile, Vector2I) GetTileOnTop()
	{
		return GetNearestHittingTile(HitCheckUpA.GlobalPosition, HitCheckUpB.GlobalPosition);
	}

	private (Tile, Vector2I) GetTileToLeft()
	{
		return GetNearestHittingTile(HitCheckLeftA.GlobalPosition, HitCheckLeftB.GlobalPosition);
	}

	private (Tile, Vector2I) GetTileToRight()
	{
		return GetNearestHittingTile(HitCheckRightA.GlobalPosition, HitCheckRightB.GlobalPosition);
	}

	private (Tile, Vector2I) GetNearestHittingTile(Vector2 checkPositionA, Vector2 checkPositionB)
	{
		Vector2I coordA = Stage.WorldToGrid(checkPositionA);
		Vector2I coordB = Stage.WorldToGrid(checkPositionB);
		
		Tile tileA = Stage.TileGrid.GetTileTouchable(coordA);
		
		if (coordA == coordB)
			return (tileA, coordA);
	
		
		// Here they're not equal so at least one of them is not null
		
		Tile tileB = Stage.TileGrid.GetTileTouchable(coordB);

		if (tileA is null)
			return (tileB, coordB);
		
		if (tileB is null)
			return (tileA, coordA);


		// Neither are null so return nearest
		
		return Stage.GridToWorld(coordA).DistanceTo(Position) < Stage.GridToWorld(coordB).DistanceTo(Position)
			? (tileA, coordA)
			: (tileB, coordB);
	}
}
