using Godot;
using TD.Connection;
using TD.Enums;
using TD.Lib;
using TD.Models;

namespace TD.Entities.CharacterControllers;

public partial class PlayerController : Node2D
{
	private static readonly Logger Logger = new("PlayerController");
	
	private Character character;
	
	private int HorizontalInput;
	private int VerticalInput;
	
	private ulong lastBumpFrame;
	private ulong lastStandFrame;
	private Tile tileStoodOn;


	private bool ShouldSendUpdate;
    
	
	public override void _Ready()
	{
		base._Ready();
		character = GetParent<Character>();
		character.BeforeUpdate += OnBeforeUpdate;
		character.AfterUpdate += OnAfterUpdate;
	}

	
	private void OnBeforeUpdate(double delta)
	{
		UpdateInputs();
        
		float normalizedDelta = (float)delta * Engine.PhysicsTicksPerSecond;
		Vector2 motion = GetMotion(normalizedDelta);
		Vector2 damp = GetDamp(normalizedDelta);

		if (motion.X != 0)
			character.Gfx.Scale = character.Gfx.Scale with { X = motion.X > 0 ? 1 : -1 };

		character.Velocity += character.Boost * normalizedDelta;
		character.Velocity += motion;
		character.Velocity *= damp;
		character.Boost = Vector2.Zero;
	}

	public void OnAfterUpdate(double delta)
	{
		if (character.Position.Y > Stage.LowerBoundary)
		{
			character.Respawn();
			ShouldSendUpdate = true;
		}

		HandleCollisions();

		character.NormalShape.Disabled = character.Velocity.Y < 0;
		character.JumpingShape.Disabled = !character.NormalShape.Disabled;


		if (Stage.RaceIsOnline)
		{
			ShouldSendUpdate |= Engine.GetPhysicsFrames() % 60 == 0 && VerticalInput != 0 && HorizontalInput != 0;
			
			if (ShouldSendUpdate)
			{
				ShouldSendUpdate = false;
			
				_ = Socket.SendRaceCharacterUpdate(
					character.Position,
					character.Velocity,
					HorizontalInput,
					VerticalInput
				);
			}
			
			// TODO: Send when altered the world
		}
	}


	private void UpdateInputs()
	{
		int currentHorizontalInput = HorizontalInput;
		int currentVerticalInput = VerticalInput;
		
		HorizontalInput = (int)Input.GetAxis("move_left", "move_right");
		VerticalInput = (int)Input.GetAxis("jump", "crouch");

		ShouldSendUpdate |= currentHorizontalInput != HorizontalInput || currentVerticalInput != VerticalInput;
	}
	
	private Vector2 GetDamp(float normalizedDelta)
	{
		float horizontalScale = tileStoodOn?.friction ?? 1;
		
		return new Vector2(
			Mathf.Pow(character.Damp.X, normalizedDelta * horizontalScale),
			Mathf.Pow(character.Damp.Y, normalizedDelta)
		);
	}
	
	private Vector2 GetMotion(float normalizedDelta)
	{
		if (character.Stunned)
			return Vector2.Zero;
		
		Vector2 motion = new();
		
		float speed = character.Speed;

		// Alongside the damp scaling based on friction, this makes the player
		// able to only move up to their regular speed on slippery blocks.
		if (tileStoodOn is not null && tileStoodOn.friction < 1)
			speed *= tileStoodOn.friction;
    
		motion += new Vector2(HorizontalInput * speed, 0);

		if (character.IsOnFloor() && VerticalInput == -1)
			motion = Vector2.Up * character.JumpPower;

		motion += Vector2.Down * character.Gravity;
		
		return motion * normalizedDelta;
	}
	
	
	private void HandleCollisions()
	{
		tileStoodOn = null;
		
		for (int i = 0; i < character.GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D collision = character.GetSlideCollision(i);
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
					if (HorizontalInput < 0 || character.Velocity.X < 0)
						TouchTileLeft();
					break;
				
				case (-1, 0):
					if (HorizontalInput > 0 || character.Velocity.X > 0)
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
		var (tile, coord) = character.GetTileOnTop();
		Stage.TriggerTileEffects(TileEffectTrigger.Bump, tile, coord, character);
	}

	private void TouchTileDown()
	{
		var (tile, coord) = character.GetTileBelow();

		if (tile is { safe: true })
			character.RespawnPoint = Game.GridToWorld(coord) + Vector2.Up * Game.Config.tileSize * 0.5f;

		tileStoodOn = tile;
		
		Stage.TriggerTileEffects(TileEffectTrigger.Stand, tile, coord, character);
	}

	private void TouchTileLeft()
	{
		var (tile, coord) = character.GetTileToLeft();
		Stage.TriggerTileEffects(TileEffectTrigger.PushLeft, tile, coord, character);
	}

	private void TouchTileRight()
	{
		var (tile, coord) = character.GetTileToRight();
		Stage.TriggerTileEffects(TileEffectTrigger.PushRight, tile, coord, character);
	}
}
