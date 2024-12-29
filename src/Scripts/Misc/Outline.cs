using Godot;
using System;

namespace TD.Misc;

public partial class Outline : Control
{
	private AnimationPlayer AnimationPlayer;
    
	
	public override void _Ready()
	{
		base._Ready();
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}


	public new void Show()
	{
		base.Show();
		AnimationPlayer.Stop();
	}

	public new void Hide()
	{
		base.Hide();
		AnimationPlayer.Stop();
	}
	
	public void Blink()
	{
		base.Show();
		AnimationPlayer.Play("blink");
	}
}
