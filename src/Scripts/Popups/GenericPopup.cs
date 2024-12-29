using Godot;

namespace TD.Popups;

public partial class GenericPopup : CanvasLayer
{
	[Signal]
	public delegate void RemovedEventHandler();


	private Control window;
	private Control content;
	private AnimationPlayer animationPlayer;

	
	public bool Canceled { get; private set; }
	private bool cancellable;


	public override void _Ready()
	{
		base._Ready();
		
		window = GetNode<Control>("%Window");
		content = GetNode<Control>("%Content");
		animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		
		Visible = false;
		animationPlayer.Play("appear");
		
		SetProcessUnhandledInput(cancellable);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Input.IsActionJustPressed("cancel"))
		{
			Cancel();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMGoBackRequest)
			Cancel();
	}

	
	public GenericPopup SetCancellable(bool value)
	{
		SetProcessUnhandledInput(value);
		cancellable = value;
		return this;
	}
	
	public void SetWidth(float value)
	{
		window.CustomMinimumSize = window.CustomMinimumSize with { X = value };
	}
	
	public void Remove()
	{
		animationPlayer.Play("remove");
		EmitSignal(SignalName.Removed);
	}
	
	public void Cancel()
	{
		if (cancellable)
		{
			Canceled = true;
			Remove();
		}
	}
	

	public void OnOutsidePressed()
	{
		Cancel();
	}
	
	public void OnAnimationPlayerAnimationFinished(StringName animName)
	{
		if (animName == "remove")
			QueueFree();
	}
}