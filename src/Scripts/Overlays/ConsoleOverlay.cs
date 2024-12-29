using Godot;
using TD.DevTools;

namespace TD.Overlays;

public partial class ConsoleOverlay : CanvasLayer
{
	private const uint MaxLineCount = 256;
	private static readonly Color InfoColor = new(0.8f, 0.8f, 0.85f);
	private static readonly Color WarnColor = new(0.9f, 0.9f, 0.5f);
	private static readonly Color ErrorColor = new(0.9f, 0.4f, 0.4f);

	private CheckButton autoScrollCheckButton;
	private RichTextLabel richTextLabel;
	
	
	public override void _Ready()
	{
		base._Ready();
		
		Hide();
		
		autoScrollCheckButton = GetNode<CheckButton>("%AutoScrollCheckButton");
		richTextLabel = GetNode<RichTextLabel>("%RichTextLabel");
		richTextLabel.Text = string.Empty;
		
		LogCapture.Instance.NewLine += OnLogCaptureNewLine;
	}

	public override void _ExitTree()
	{
		LogCapture.Instance.NewLine -= OnLogCaptureNewLine;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);
		if (Input.IsActionJustPressed("toggle_console"))
		{
			Visible = !Visible;
			GetViewport().SetInputAsHandled();
		}
	}


	private void OnLogCaptureNewLine(string line)
	{
		richTextLabel.AppendText(line + "\n");
		
		if (autoScrollCheckButton.IsPressed())
			richTextLabel.ScrollToLine(richTextLabel.GetLineCount() - 1);
		
		while (richTextLabel.GetLineCount() > MaxLineCount)
			richTextLabel.RemoveParagraph(0);
	}
}
