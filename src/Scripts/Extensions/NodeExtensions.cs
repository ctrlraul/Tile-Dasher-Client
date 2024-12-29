using Godot;

namespace TD.Extensions;

public static class NodeExtensions
{
	public static void QueueFreeChildren(this Node node)
	{
		foreach (Node child in node.GetChildren())
			child.QueueFree();
	}
}