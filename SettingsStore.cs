using System.Text.Json;
using Microsoft.Win32;

namespace WifiEmulatorAccept;

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public bool RunAtStartup { get; set; }
    public string ExtraTitleContains { get; set; } = "";
}

public static class SettingsStore
{
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WifiEmulatorAccept");

    static string FilePath => Path.Combine(Dir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();
            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
            return loaded ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}

public static class StartupManager
{
    const string ValueName = "WifiEmulatorAccept";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return !string.IsNullOrWhiteSpace(key?.GetValue(ValueName) as string);
    }

    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("Could not open the startup registry key.");

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            return;
        key.SetValue(ValueName, $"\"{exe}\" --tray");
    }
}
