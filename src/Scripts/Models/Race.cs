using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TD.Models;

[JsonObject]
public class Race {
	public DateTime startTime;
	public List<PlayerProfile> players;
	public Track track;
}