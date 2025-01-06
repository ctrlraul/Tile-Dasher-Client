using Godot;
using TD.Enums;
using TD.Lib;
using TD.Models;

namespace TD;

public partial class Character : CharacterBody2D
{
	private static readonly Logger Logger = new("Character");
    
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
	public RemoteTransform2D RemoteTransform;
	private Timer StunTimer;
	private AnimationPlayer AnimationPlayer;

	private ulong lastBumpFrame;
	private ulong lastStandFrame;
	private Tile tileStoodOn;
	public Vector2 RespawnPoint;
	public string PlayerName { get; private set; }
	public Vector2 Boost;
	public bool Finished { get; private set; }
	public long LastTeleportTimeMs;
	private float HorizontalInput;
	public Vector2 VelocityLastUpdate { get; private set; }
	public Vector2I Coord => Game.WorldToGrid(Position);
	public bool Stunned => StunTimer.TimeLeft > 0;


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
		RemoteTransform = GetNode<RemoteTransform2D>("%RemoteTransform2D");
		StunTimer = GetNode<Timer>("%StunTimer");
		AnimationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		SetPlayerName(null);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		VelocityLastUpdate = Velocity;

		float normalizedDelta = (float)delta * Engine.PhysicsTicksPerSecond;
		Vector2 motion = GetMotion(normalizedDelta);
		Vector2 damp = GetDamp(normalizedDelta);

		if (motion.X != 0)
			Gfx.Scale = Gfx.Scale with { X = motion.X > 0 ? 1 : -1 };

		Velocity += Boost * normalizedDelta;
		Velocity += motion;
		Velocity *= damp;
		
		Boost = Vector2.Zero;
		
		MoveAndSlide();

		if (Position.Y > Stage.LowerBoundary)
			Respawn();

		HandleCollisions();

		NormalShape.Disabled = Velocity.Y < 0;
		JumpingShape.Disabled = !NormalShape.Disabled;
	}

	
	private Vector2 GetMotion(float normalizedDelta)
	{
		Vector2 motion = new();

		if (!Stunned)
		{
			HorizontalInput = Input.GetAxis("move_left", "move_right");
			float speed = Speed;

			// Alongside the damp scaling based on friction, this makes the player
			// able to only move up to their regular speed on slippery blocks.
			if (tileStoodOn is not null && tileStoodOn.friction < 1)
				speed *= tileStoodOn.friction;
        
			motion += new Vector2(HorizontalInput * speed, 0);

			if (IsOnFloor() && Input.IsActionPressed("jump"))
				motion += Vector2.Up * JumpPower;
		}

		motion += Vector2.Down * Gravity;
		
		return motion * normalizedDelta;
	}

	private Vector2 GetDamp(float normalizedDelta)
	{
		float horizontalScale = tileStoodOn?.friction ?? 1;
		
		return new Vector2(
			Mathf.Pow(Damp.X, normalizedDelta * horizontalScale),
			Mathf.Pow(Damp.Y, normalizedDelta)
		);
	}

	private void HandleCollisions()
	{
		tileStoodOn = null;
		
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			Vector2 normal = collision.GetNormal().Round();

			switch (normal.X, normal.Y)
			{
				case (0, 1):
					if (Engine.GetPhysicsFrames() != lastBumpFrame)
					{
						lastBumpFrame = Engine.GetPhysicsFrames();
						TouchTileUp();
					}
					break;
				
				case (0, -1):
					if (Engine.GetPhysicsFrames() != lastStandFrame)
					{
						lastStandFrame = Engine.GetPhysicsFrames();
						TouchTileDown();
					}
					break;
				
				case (1, 0):
					if (HorizontalInput < 0 || Velocity.X < 0)
						TouchTileLeft();
					break;
				
				case (-1, 0):
					if (HorizontalInput > 0 || Velocity.X > 0)
						TouchTileRight();
					break;
				
				default:
					Logger.Log("Weird contact: ", normal);
					break;
			}
		}
	}
	
	private void TouchTileUp()
	{
		var (tile, coord) = GetTileOnTop();
		Stage.TriggerTileEffects(TileEffectTrigger.Bump, tile, coord, this);
	}

	private void TouchTileDown()
	{
		var (tile, coord) = GetTileBelow();

		if (tile is { safe: true })
			RespawnPoint = Game.GridToWorld(coord) + Vector2.Up * Game.Config.tileSize * 0.5f;

		tileStoodOn = tile;
		
		Stage.TriggerTileEffects(TileEffectTrigger.Stand, tile, coord, this);
	}

	private void TouchTileLeft()
	{
		var (tile, coord) = GetTileToLeft();
		Stage.TriggerTileEffects(TileEffectTrigger.PushLeft, tile, coord, this);
	}

	private void TouchTileRight()
	{
		var (tile, coord) = GetTileToRight();
		Stage.TriggerTileEffects(TileEffectTrigger.PushRight, tile, coord, this);
	}


	public void SetPlayerName(string name)
	{
		PlayerName = name;
		NameLabel.Text = name;
		NameLabel.Visible = !string.IsNullOrEmpty(name);
	}

	public void Finish()
	{
		if (Finished)
			return;
		
		Finished = true;
		
		SetPhysicsProcess(false);
		
		Tween tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 0, 1);
		tween.Finished += QueueFree;
	}
	
	public void Respawn()
	{
		Position = RespawnPoint;
		Velocity = Vector2.Zero;
	}

	public void Stun()
	{
		if (Stunned)
			return;
        
		AnimationPlayer.Play("Stun");
		StunTimer.Start();
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
		Vector2I coordA = Game.WorldToGrid(checkPositionA);
		Vector2I coordB = Game.WorldToGrid(checkPositionB);
		
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
		
		return Game.GridToWorld(coordA).DistanceTo(Position) < Game.GridToWorld(coordB).DistanceTo(Position)
			? (tileA, coordA)
			: (tileB, coordB);
	}


	private void OnStunTimerTimeout()
	{
		AnimationPlayer.Stop();
	}
}
