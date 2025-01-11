using System;
using Godot;
using TD.Entities.CharacterControllers;
using TD.Models;

namespace TD.Entities;

public partial class Character : CharacterBody2D
{
	public enum Controller
	{
		Player,
		Socket,
	}
	
	
	public event Action<double> BeforeUpdate; 
	public event Action<double> AfterUpdate;
	
	
	public readonly float Speed = 90;
	public readonly float JumpPower = 1125;
	public readonly float Gravity = 23.5f;
	public readonly Vector2 Damp = new(0.85f, 0.98f);


	public Node2D Gfx;
	public CollisionShape2D NormalShape;
	public CollisionShape2D JumpingShape;
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

	public Vector2 RespawnPoint;
	public string PlayerName { get; private set; }
	public string PlayerId { get; private set; }
	public Vector2 Boost;
	public bool Finished { get; private set; }
	public long LastTeleportTimeMs;
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
		Clear();
	} 
    
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
		VelocityLastUpdate = Velocity;
		
		BeforeUpdate?.Invoke(delta);
		
		MoveAndSlide();
		
		AfterUpdate?.Invoke(delta);
	}


	private void Clear()
	{
		PlayerId = null;
		PlayerName = null;
		NameLabel.Text = null;
	}

	public void AddController(Controller controller)
	{
		switch (controller)
		{
			case Controller.Player:
				AddChild(new PlayerController());
				break;
			
			case Controller.Socket:
				AddChild(new SocketController());
				break;
		}
	}

	public void SetPlayer(PlayerProfile player, bool showName = false)
	{
		PlayerId = player.id;
		PlayerName = player.name;
		
		if (showName)
			NameLabel.Text = player.name;
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
	

	public (Tile, Vector2I) GetTileBelow()
	{
		return GetNearestHittingTile(HitCheckDownA.GlobalPosition, HitCheckDownB.GlobalPosition);
	}

	public (Tile, Vector2I) GetTileOnTop()
	{
		return GetNearestHittingTile(HitCheckUpA.GlobalPosition, HitCheckUpB.GlobalPosition);
	}

	public (Tile, Vector2I) GetTileToLeft()
	{
		return GetNearestHittingTile(HitCheckLeftA.GlobalPosition, HitCheckLeftB.GlobalPosition);
	}

	public (Tile, Vector2I) GetTileToRight()
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
