namespace WifiEmulatorAccept;

public static class SelfTest
{
    public static int Run()
    {
        var failures = new List<string>();
        Check(failures, "windows allow access", WindowsPromptMatch.IsAllowButton("Allow access"));
        Check(failures, "windows dont allow", !WindowsPromptMatch.IsAllowButton("Don't allow"));
        Check(failures, "windows cancel", !WindowsPromptMatch.IsAllowButton("Cancel"));
        Check(failures, "firewall bluestacks", WindowsPromptMatch.MentionsEmulator("ApplicationFrameHost", "Windows Defender Firewall has blocked some features of BlueStacks", ""));
        Check(failures, "firewall chrome", !WindowsPromptMatch.MentionsEmulator("chrome", "Windows Defender Firewall has blocked some features of Google Chrome", ""));
        Check(failures, "title word emulator", !WindowsPromptMatch.MentionsEmulator("chrome", "Windows Wi-Fi emulator auto-accept app - grok", ""));
        Check(failures, "explorer folder", !WindowsPromptMatch.MentionsEmulator("explorer", "WifiEmulatorAccept - File Explorer", ""));
        Check(failures, "equinox", !WindowsPromptMatch.MentionsEmulator("notepad", "Equinox", ""));
        Check(failures, "android emulator", WindowsPromptMatch.MentionsEmulator("ApplicationFrameHost", "Windows Security Android Emulator", ""));
        Check(failures, "extra title", WindowsPromptMatch.MentionsEmulator("notepad", "Pixel_6 is open", "Pixel_6"));
        Check(failures, "skip uac", !WindowsPromptMatch.MentionsEmulator("consent", "qemu-system wants to make changes", ""));
        Check(failures, "emulator process", WindowsPromptMatch.MentionsEmulator("HD-Player", "", ""));
        try
        {
            var root = System.Windows.Automation.AutomationElement.RootElement;
            Check(failures, "windows ui automation", root != null);
        }
        catch (Exception ex)
        {
            failures.Add("windows ui automation " + ex.GetType().Name + ": " + ex.Message);
        }

        var wifi = WifiStatus.Read();
        if (string.IsNullOrWhiteSpace(wifi.Text))
            failures.Add("wifi status text");
        var described = AppLog.Describe(new AppSettings { Enabled = true, RunAtStartup = false }, wifi);
        if (!described.Contains(wifi.Text, StringComparison.Ordinal)
            || !described.Contains("Watching is on", StringComparison.Ordinal)
            || !described.Contains("Starts only when opened", StringComparison.Ordinal))
            failures.Add("status log");
        var report = Path.Combine(Path.GetTempPath(), "WifiEmulatorAccept-selftest.txt");
        var body = failures.Count == 0 ? "passed" : string.Join(Environment.NewLine, failures);
        File.WriteAllText(report, body + Environment.NewLine + wifi.Text);
        return failures.Count == 0 ? 0 : 1;
    }

    static void Check(List<string> failures, string name, bool ok)
    {
        if (!ok)
            failures.Add(name);
    }
}
