using Godot;
using System;

namespace TD.Tools;

public partial class DraggableUi : Control
{
    [Export] public Control target;
    
    private bool dragging;
    
    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButtonEvent:
                dragging = mouseButtonEvent.Pressed;
                GetViewport().SetInputAsHandled();
                break;
            
            case InputEventMouseMotion mouseMotionEvent when dragging:
                
                target.Position += mouseMotionEvent.Relative;
                target.Position = new Vector2(
                    Math.Clamp(target.Position.X, 0, GetViewportRect().Size.X - target.Size.X),
                    Math.Clamp(target.Position.Y, 0, GetViewportRect().Size.Y - target.Size.Y)
                );
                
                GetViewport().SetInputAsHandled();
                
                break;
        }
    }
}
