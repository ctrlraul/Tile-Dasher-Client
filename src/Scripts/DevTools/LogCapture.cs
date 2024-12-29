using System;
using System.IO;
using Godot;
using TD.Exceptions;

namespace TD.DevTools;

public partial class LogCapture : Node
{
    public static LogCapture Instance { get; private set; }

    public event Action<string> NewLine;
    
    private FileStream logStream;
    private StreamReader logReader;

    public override void _Ready()
    {
        if (Instance != null)
            throw new SingletonException(typeof(LogCapture));

        Instance = this;
    }

    public override void _EnterTree()
    {
        string logPath = ProjectSettings.GetSetting("debug/file_logging/log_path").AsString();
        string path = ProjectSettings.GlobalizePath(logPath);
        logStream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
        logReader = new StreamReader(logStream);
    }

    public override void _ExitTree()
    {
        logReader.Dispose();
        logStream.Dispose();
    }

    public override void _Process(double _delta)
    {
        while (!logReader.EndOfStream)
        {
            string line = logReader.ReadLine();
            
            if (line is null)
                continue;

            NewLine?.Invoke(line);
        }
    }
}