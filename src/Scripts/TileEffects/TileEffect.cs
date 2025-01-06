namespace TD.TileEffects;

public class TileEffect
{
	/// <summary>
	/// Causes this effect to make a tile's collision shape to be excluded from terrain merging
	/// </summary>
	public virtual bool PopOff { get; } = false;
	public virtual bool NeedIndexing { get; } = false;

	public virtual void Trigger(TriggerData data)
	{
	}
}