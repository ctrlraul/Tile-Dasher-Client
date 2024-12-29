using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TD.Models;

namespace TD.TileEffects;

public class TileEffectVanish : ITileEffect
{
	private const int VanishTimeMs = 500;
	private const int WaitTimeMs = 2000;
	private const int AppearTimeMs = 500;
    
	private class OngoingEffect
	{
		public event Action Finished;
		
		private long timeOfAppearing;
		private readonly CollisionShape2D shape;
		private readonly Sprite2D sprite;
		private readonly Area2D characterCheck;
		private Tween tween;
        
		public OngoingEffect(Vector2I coord)
		{
			shape = Stage.TileGrid.GetCollisionShape(coord);
			sprite = Stage.TileGrid.GetSprite(coord);
			characterCheck = new Area2D();

			characterCheck.CollisionLayer = 0;
			characterCheck.CollisionMask = 1; // Characters
			characterCheck.AddChild(shape.Duplicate());
			Stage.Temp.AddChild(characterCheck);

			Vanish();
		}
		
		public void ReTrigger()
		{
			if (timeOfAppearing == 0)
				return;

			long timePassedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeOfAppearing;
			timeOfAppearing = 0;
			
			tween.Stop();
			Vanish(timePassedMs);
		}


		private void Vanish(float? timeToHideOverrideMs = null)
		{
			double time = (timeToHideOverrideMs ?? VanishTimeMs) / 1000.0;
            
			tween = sprite.CreateTween();
			tween.TweenProperty(sprite, "self_modulate:a", 0,  time);
			tween.Finished += () =>
			{
				shape.Disabled = true;
				WaitToAppear();
			};
		}

		private async void WaitToAppear()
		{
			await Task.Delay(WaitTimeMs);

			if (characterCheck.HasOverlappingBodies())
			{
				WaitToAppear();
				return;
			}

			timeOfAppearing = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			shape.Disabled = false;
			
			tween = sprite.CreateTween();
			tween.TweenProperty(sprite, "self_modulate:a", 1, AppearTimeMs / 1000.0);
			tween.Finished += () =>
			{
				characterCheck.QueueFree();
				Finished?.Invoke();
			};
		}
	}
	
	private static readonly Dictionary<Vector2I, OngoingEffect> OngoingEffects = new();
	
	
	public bool NonStatic { get; private set; } = true;
	
	public void Trigger(Tile tile, Vector2I coord, Character character)
	{
		if (OngoingEffects.TryGetValue(coord, out OngoingEffect effect))
		{
			effect.ReTrigger();
		}
		else
		{
			effect = new OngoingEffect(coord);
			effect.Finished += () => OngoingEffects.Remove(coord);
			OngoingEffects.Add(coord, effect);
		}
	}
}
