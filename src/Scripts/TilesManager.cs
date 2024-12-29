using System.Collections.Generic;
using TD.Enums;
using TD.Models;

namespace TD;

public abstract class TilesManager
{
	private static readonly List<Tile> Tiles = new();
    private static readonly HashSet<Tile> NonStaticTiles = new();
	
	
	public static void SetTiles(List<Tile> tiles)
	{
		Tiles.Clear();
		NonStaticTiles.Clear();

		foreach (Tile tile in tiles)
		{
			foreach (List<string> effectIds in tile.effects.Values)
			{
				foreach (string effectId in effectIds)
				{
					if (Game.TileEffects[effectId].NonStatic)
					{
						NonStaticTiles.Add(tile);
						break;
					}
				}

				if (NonStaticTiles.Contains(tile))
					break;
			}
		}
        
		// TODO: Check if tiles are all valid here
        
		Tiles.AddRange(tiles);
	}

	public static bool IsStatic(Tile tile)
	{
		return !NonStaticTiles.Contains(tile);
	}
}