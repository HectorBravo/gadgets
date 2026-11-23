using System.IO;
using System.Text.Json;

namespace HectorBravo.Gadgets.Core;

/// <summary>Settings every gadget shares (window geometry + common menu state).</summary>
public class BaseSettings
{
    public double? X { get; set; }
    public double? Y { get; set; }
    public double Width { get; set; } = 250;
    public double Height { get; set; } = 430;
    public bool AlwaysOnTop { get; set; } = true;
    /// UI scale: 1.0, 1.25, 1.5 or 2.0 (LayoutTransform on the root).
    public double Scale { get; set; } = 1.0;
    /// Window opacity: 0.2 … 1.0.
    public double Opacity { get; set; } = 0.85;
}

/// <summary>JSON config persisted at %APPDATA%\&lt;AppName&gt;\config.json (settings only).</summary>
public class ConfigStore<T> where T : new()
{
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public string Path { get; }
    public T Data { get; set; }

    public ConfigStore(string appName)
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
        Path = System.IO.Path.Combine(dir, "config.json");
        try
        {
            if (File.Exists(Path))
                Data = JsonSerializer.Deserialize<T>(File.ReadAllText(Path)) ?? new T();
            else
                Data = new T();
        }
        catch
        {
            // corrupt config → keep a copy and start fresh
            try { File.Move(Path, Path + ".bak", overwrite: true); } catch { /* best effort */ }
            Data = new T();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, JsonSerializer.Serialize(Data, Opts));
        }
        catch { /* never crash a gadget over its config file */ }
    }
}
