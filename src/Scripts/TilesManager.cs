using System.Collections.Generic;
using Godot;
using TD.Enums;
using TD.Models;
using TD.TileEffects;

namespace TD;

public abstract class TilesManager
{
	private static readonly List<Tile> Tiles = new();
	private static readonly HashSet<Tile> TilesThatPopOff = new();
	private static readonly HashSet<Tile> TilesThatNeedIndexing = new();
	private static readonly Dictionary<long, Texture2D> TileTextures = new();
	private static readonly Dictionary<long, Color> TileColors = new();
	public static readonly Dictionary<string, TileEffect> Effects = new();


	static TilesManager()
	{
		Effects.Add("break", new TileEffectBreak());
		Effects.Add("vanish", new TileEffectVanish());
		Effects.Add("finish", new TileEffectFinish());
		Effects.Add("boost_left", new TileEffectBoostLeft());
		Effects.Add("boost_right", new TileEffectBoostRight());
		Effects.Add("boost_up", new TileEffectBoostUp());
		Effects.Add("boost_down", new TileEffectBoostDown());
		Effects.Add("teleport", new TileEffectTeleport());
		Effects.Add("crumble", new TileEffectCrumble());
		Effects.Add("push", new TileEffectPush());
		Effects.Add("respawn", new TileEffectRespawn());
		Effects.Add("launch", new TileEffectLaunch());
		Effects.Add("stun", new TileEffectStun());
		Effects.Add("wander", new TileEffectWander());
	}
    
	
	public static void SetTiles(List<Tile> tiles)
	{
		// TODO: Check if tiles are all valid before doing anything here
		
		Tiles.Clear();
		TilesThatPopOff.Clear();
		TilesThatNeedIndexing.Clear();
		TileTextures.Clear();
		TileColors.Clear();

		Texture2D atlas = Game.GetTileSet();
		Vector2I tileSize = Vector2I.One * Game.Config.tileSize;
		
		foreach (Tile tile in tiles)
		{
			foreach (TileEffectConfig config in tile.effects)
			{
				TileEffect effect = Effects[config.effect];
				
				if (effect.PopOff)
					TilesThatPopOff.Add(tile);
				
				if (effect.NeedIndexing)
					TilesThatNeedIndexing.Add(tile);
			}
			
			Rect2I region = new Rect2I(tile.AtlasCoords * Game.Config.tileSize, tileSize);
			Image image = ImageUtils.Crop(atlas.GetImage(), region);
			Color color = ImageUtils.GetAverageColor(image, 110);
			Texture2D texture = ImageTexture.CreateFromImage(image);
			TileTextures.Add(tile.id, texture);
			TileColors.Add(tile.id, color);
		}
        
		Tiles.AddRange(tiles);
	}

	public static bool PopsOff(Tile tile)
	{
		return TilesThatPopOff.Contains(tile);
	}

	public static bool NeedsIndexing(Tile tile)
	{
		return TilesThatNeedIndexing.Contains(tile);
	}

	public static Texture2D GetTexture(Tile tile)
	{
		return TileTextures.GetValueOrDefault(tile.id);
	}

	public static Color GetColor(long id)
	{
		return TileColors.GetValueOrDefault(id);
	}
}

public abstract class ImageUtils
{
	public static Image Crop(Image source, Rect2I region)
	{
		Image cropped = Image.CreateEmpty(
			region.Size.X,
			region.Size.Y,
			source.HasMipmaps(),
			source.GetFormat()
		);

		cropped.BlitRect(source, region, Vector2I.Zero);
		
		return cropped;
	}

	public static Color GetAverageColor(Image source, int step)
	{
		Vector2I size = source.GetSize();

		int samples = 1;
		float r = 0;
		float g = 0;
		float b = 0;
		float a = 0;

		for (int i = 0; i < size.X * size.Y; i += step)
		{
			samples++;
			int x = i % size.X;
			int y = i / size.Y;
			
			Color color = source.GetPixel(x, y);
			r += color.R;
			g += color.G;
			b += color.B;
			a += color.A;
			
			// source.SetPixel(x, y, Colors.Red);
		}

		return new Color(r / samples, g / samples, b / samples, a / samples);
	}
}