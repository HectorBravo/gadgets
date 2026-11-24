using System.IO;
using System.Text.Json;

namespace HectorBravo.Gadgets.Core;

/// <summary>Settings every gadget shares (window geometry + common menu state).</summary>
public class BaseSettings
{
    public double? X { get; set; }
    public double? Y { get; set; }
    /// UI scale: 1.0, 1.25, 1.5 or 2.0 (LayoutTransform on the root → resizes the window).
    public double Scale { get; set; } = 1.0;
    /// Window opacity: 0.2 … 1.0 (the panel itself is fully opaque).
    public double Opacity { get; set; } = 1.0;
    /// Gadgets are NOT always on top by default.
    public bool AlwaysOnTop { get; set; } = false;
    /// Graph height in DIP (all gadgets that draw graphs).
    public double GraphHeight { get; set; } = 60;
}

/// <summary>
/// JSON config persisted at %APPDATA%\&lt;AppName&gt;\config.json. ONE store per gadget
/// (shared by the base window and the gadget) so nothing overwrites another writer.
/// Every change is saved immediately (Save() right after mutating Data).
/// </summary>
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
            var tmp = Path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Data, Opts));
            File.Move(tmp, Path, overwrite: true);   // atomic-ish: config never lost half-written
        }
        catch { /* never crash a gadget over its config file */ }
    }
}

