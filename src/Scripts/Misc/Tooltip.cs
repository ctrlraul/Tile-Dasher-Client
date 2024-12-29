using Godot;

namespace MechGame.Misc;

[Tool]
public partial class Tooltip : Control
{
	private const float Margin = 10;
	private Label Label;
	
	private string text = "Tooltip";
	
	[Export]
	public string Text
	{
		get => text;
		set
		{
			text = value;
			UpdateContent();
		}
	}


	public override void _Ready()
	{
		base._Ready();
		Label = GetNode<Label>("%Label");
		
		UpdateContent();

		if (!Engine.IsEditorHint())
		{
			Hide();
			
			Control parent = GetParent<Control>();
			
			parent.MouseEntered += () =>
			{
				CallDeferred(CanvasItem.MethodName.Show);
				SetProcessInput(true);
			};
			
			parent.MouseExited += () =>
			{
				Hide();
				SetProcessInput(false);
			};
		}
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (@event is InputEventMouseMotion)
			UpdatePosition();
	}


	private void UpdateContent()
	{
		if (Label != null)
			Label.Text = text;
	}

	private void UpdatePosition()
	{
		Vector2 position = GetGlobalMousePosition();
		Vector2 viewportSize = GetViewportRect().Size;

		position.X += position.X > viewportSize.X * 0.5f ? -Size.X - Margin : Margin;
		position.Y += position.Y > viewportSize.Y * 0.5f ? -Size.Y - Margin : Margin;
		
		GlobalPosition = position;
	}
}
