using System;
using Godot;
using TD.Extensions;

namespace TD.Popups;

public partial class DialogPopup : GenericPopup
{
    public enum Style
    {
        Info,
        Warn,
        Error,
    }
    
    private const float ButtonWidth = 100;
    private const float MaxMessageLabelHeight = 720;

    protected Label titleLabel;
    protected Control spinnerContainer;
    protected PanelContainer messageContainer;
    protected RichTextLabel messageLabel;
    protected HBoxContainer buttonsContainer;
    protected Control customContainer;

    private Vector2 initialMessageLabelCustomMinimumSize;
    

    public override void _Ready()
    {
        base._Ready();

        titleLabel = GetNode<Label>("%TitleLabel");
        spinnerContainer = GetNode<Control>("%SpinnerContainer");
        messageContainer = GetNode<PanelContainer>("%MessageContainer");
        messageLabel = GetNode<RichTextLabel>("%MessageLabel");
        buttonsContainer = GetNode<HBoxContainer>("%ButtonsContainer");
        customContainer = GetNode<Control>("%CustomContainer");

        initialMessageLabelCustomMinimumSize = messageLabel.CustomMinimumSize;

        messageLabel.ItemRectChanged += OnMessageLabelItemRectChanged;
        
        Clear();
    }

    
    private void Clear()
    {
        SetTitle(null);
        SetMessage(null);
        SetSpinner(false);
        SetCustomContent(null);
        SetStyle(Style.Info);
        RemoveButtons();
    }
    
    private void RemoveButtons()
    {
        buttonsContainer.Visible = false;
        foreach (Node child in buttonsContainer.GetChildren())
        {
            buttonsContainer.RemoveChild(child);
            child.QueueFree();
        }
    }
    

    public DialogPopup SetTitle(string title)
    {
        titleLabel.Visible = !string.IsNullOrEmpty(title);
        titleLabel.Text = title;
        return this;
    }

    public DialogPopup SetMessage(string message)
    {
        messageContainer.Visible = !string.IsNullOrEmpty(message);
        messageLabel.Text = message;
        return this;
    }

    public DialogPopup SetSpinner(bool enabled)
    {
        spinnerContainer.Visible = enabled;
        return this;
    }

    public DialogPopup SetCustomContent(Node customContent)
    {
        customContainer.Visible = customContent != null;
        customContainer.QueueFreeChildren();
        
        if (customContainer.Visible)
            customContainer.AddChild(customContent);
        
        return this;
    }

    public DialogPopup SetStyle(Style style)
    {
        return this;
    }
    
    public DialogPopup AddButton(string text, Action OnPressed = null)
    {
        Button button = new()
        {
            Text = text,
            CustomMinimumSize = new Vector2(ButtonWidth, 0)
        };

        if (buttonsContainer.GetChildCount() > 0)
            buttonsContainer.AddSpacer(false);

        buttonsContainer.Visible = true;
        buttonsContainer.AddChild(button);
        
        if (OnPressed == null)
        {
            button.Pressed += Remove;
        }
        else
        {
            button.Pressed += () =>
            {
                OnPressed.Invoke();
                Remove();
            };
        }
        
        return this;
    }

    
    private void OnMessageLabelItemRectChanged()
    {
        messageLabel.FitContent = messageLabel.GetContentHeight() < MaxMessageLabelHeight;

        if (messageLabel.FitContent)
            messageLabel.CustomMinimumSize = initialMessageLabelCustomMinimumSize;
        else
            messageLabel.CustomMinimumSize = messageLabel.CustomMinimumSize with { Y = MaxMessageLabelHeight };
    }
}