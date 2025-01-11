using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class Race {
	public long startTime;
	public List<PlayerProfile> players;
	public Track track;
	public string type;
}