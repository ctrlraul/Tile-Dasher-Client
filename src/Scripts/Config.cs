using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace TD;

[JsonObject]
public class Config
{
	public string serverUrl = "http://localhost:4200/";
	public string webSocketServerUrl = "ws://localhost:4201";


	public int tileSize = 80;
	public int spawnTileId = 3;
	
	
	// Storage

	public string tracksFolder = "user://tracks/";
	
	
	// Debug
	
	public List<string> multiplayerTestPlayerIds = new();
	public bool showDragNodes;
	
	
	// Theming
	
	public uint colorGood = 0x9aff76ff;
	public uint colorBad = 0xff2b5dff;
	public uint colorPhysical = 0xffd042ff;
	public uint colorHeat = 0xff3d2bff;
	public uint colorEnergy = 0x4f5effff;
	
	public Color ColorGood => new(colorGood);
	public Color ColorBad => new(colorBad);
	public Color ColorPhysical => new(colorPhysical);
	public Color ColorHeat => new(colorHeat);
	public Color ColorEnergy => new(colorEnergy);
}