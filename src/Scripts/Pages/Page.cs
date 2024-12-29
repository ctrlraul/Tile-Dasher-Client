using Godot;
using TD.Lib;

namespace TD.Pages;

public partial class Page : CanvasLayer
{ 
	protected Logger Logger { get; private set; }

	protected Page()
	{
		Logger = new Logger(GetType().Name);
	}

	public virtual void Refresh()
	{
		Logger.Log("Refreshing");
	}

	public virtual void SetData(object data)
	{
	}
}