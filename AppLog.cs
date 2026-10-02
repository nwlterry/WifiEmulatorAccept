namespace WifiEmulatorAccept;

public static class AppLog
{
    public static event Action<string>? Written;

    static readonly object Gate = new();

    public static string Describe(AppSettings settings, WifiState wifi)
    {
        var watch = settings.Enabled ? "Watching is on" : "Watching is paused";
        var start = settings.RunAtStartup ? "Starts at sign-in" : "Starts only when opened";
        var titles = string.IsNullOrWhiteSpace(settings.ExtraTitleContains)
            ? "No extra window titles"
            : "Extra window titles: " + settings.ExtraTitleContains.Trim();
        return $"{watch}. {wifi.Text}. {start}. {titles}.";
    }

    public static string StatusText(AppSettings settings, WifiState wifi)
    {
        var watch = settings.Enabled ? "Watching is on" : "Watching is paused";
        var start = settings.RunAtStartup ? "Starts at sign-in" : "Starts only when opened";
        var titles = string.IsNullOrWhiteSpace(settings.ExtraTitleContains)
            ? "No extra window titles."
            : "Extra window titles: " + settings.ExtraTitleContains.Trim() + ".";
        return $"{wifi.Text}{Environment.NewLine}{watch}. {start}. {titles}";
    }

    public static bool IsStale(string line) =>
        line.Contains("belongs to the emulator", StringComparison.Ordinal)
        || line.Contains("adb found", StringComparison.Ordinal)
        || line.Contains("No emulator window is visible", StringComparison.Ordinal);

    public static string WithoutStaleLines(string text)
    {
        var kept = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !IsStale(line));
        return string.Join(Environment.NewLine, kept).Trim();
    }

    public static void Write(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss}  {message}";
        try
        {
            lock (Gate)
            {
                var dir = SettingsStore.Dir;
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "log.txt");
                var info = new FileInfo(path);
                if (info.Exists && info.Length > 1_000_000)
                {
                    var text = File.ReadAllText(path);
                    var keep = text.Length > 200_000 ? text[^200_000..] : text;
                    File.WriteAllText(path, keep);
                }

                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // The on-screen log still receives the line.
        }

        Written?.Invoke(line);
    }

    public static void ReplaceStaleLog()
    {
        try
        {
            lock (Gate)
            {
                var path = Path.Combine(SettingsStore.Dir, "log.txt");
                if (!File.Exists(path))
                    return;
                var text = File.ReadAllText(path);
                if (text.Contains("belongs to the emulator", StringComparison.Ordinal)
                    || text.Contains("adb found", StringComparison.Ordinal)
                    || text.Contains("No emulator window is visible", StringComparison.Ordinal))
                {
                    File.WriteAllText(path, "");
                }
            }
        }
        catch
        {
            // The next status line still records the current state.
        }
    }
}
