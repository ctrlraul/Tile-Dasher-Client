using Godot;
using System;

namespace TD.Misc;

public partial class InputFilter : Control
{
	public event Action FilterClicked;
    
	public void Appear()
	{
		GetNode<AnimationPlayer>("AnimationPlayer").Play("appear");
	}
	
	public void Disappear()
	{
		GetNode<AnimationPlayer>("AnimationPlayer").Play("disappear");
	}
	
	private void OnFilterClicked()
	{
		FilterClicked?.Invoke();
	}
}
