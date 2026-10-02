namespace WifiEmulatorAccept;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Any(a => a.Equals("--self-test", StringComparison.OrdinalIgnoreCase)))
            return SelfTest.Run();

        if (args.Any(a => a.Equals("--log-status", StringComparison.OrdinalIgnoreCase)))
        {
            var settings = SettingsStore.Load();
            settings.RunAtStartup = StartupManager.IsEnabled();
            var running = System.Diagnostics.Process.GetProcessesByName("WifiEmulatorAccept")
                .Any(process => process.Id != Environment.ProcessId);
            AppLog.ReplaceStaleLog();
            var state = running ? "The app is running." : "The app is not running.";
            AppLog.Write($"{state} {AppLog.Describe(settings, WifiStatus.Read())}");
            return 0;
        }

        var writeIcon = Array.FindIndex(args, a => a.Equals("--write-icon", StringComparison.OrdinalIgnoreCase));
        if (writeIcon >= 0)
        {
            var path = writeIcon + 1 < args.Length
                ? args[writeIcon + 1]
                : Path.Combine(AppContext.BaseDirectory, "App.ico");
            WifiIcons.SaveAppIcon(path);
            return 0;
        }

        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, @"Local\WifiEmulatorAccept.SingleInstance", out var created);
        if (!created)
        {
            MessageBox.Show(
                "Wi-Fi Emulator Accept is already running. Check the system tray.",
                "Wi-Fi Emulator Accept",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => WriteCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => WriteCrash(e.ExceptionObject as Exception);

        var tray = args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
        try
        {
            Application.Run(new MainForm(tray));
        }
        catch (Exception ex)
        {
            WriteCrash(ex);
            return 1;
        }

        return 0;
    }

    static void WriteCrash(Exception? ex)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WifiEmulatorAccept");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "crash.txt"), ex?.ToString() ?? "unknown crash");
        }
        catch
        {
            // Nothing else can report this.
        }
    }
}
