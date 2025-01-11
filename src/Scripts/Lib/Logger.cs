using System;
using System.Linq;
using Godot;
using Newtonsoft.Json;

namespace TD.Lib;

public class Logger
{
    public static string ColorHex { get; set; }

    private static string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

    private readonly string label;


    public Logger(string label)
    {
        this.label = label;
    }

    public void Log(params object[] message)
    {
        GD.PrintRich(Format(string.Join(" ", message)));
    }

    // public void Error(object message)
    // {
    //     GD.PrintErr(Format(message));
    // }
    
    public void Json(object obj)
    {
        Log(JsonConvert.SerializeObject(obj, Formatting.Indented));
    }


    private string Format(object message)
    {
        string buff = string.Concat(Enumerable.Repeat(" ", Math.Max(0, 20 - label.Length)));
        string text = $"{CurrentTime} {buff}{label} | {message}";

        if (ColorHex is not null)
            text = $"[color={ColorHex}]{text}[/color]";
        
        return text;
    }
}