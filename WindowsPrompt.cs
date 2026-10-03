using System.Windows.Automation;

namespace WifiEmulatorAccept;

public readonly record struct WindowsPromptResult(bool Clicked, string Label, IReadOnlyList<string> Messages);

public static class WindowsPromptMatch
{
    static readonly string[] AllowButtons = ["allow access", "allow", "accept", "允许访问", "允许", "接受", "同意"];

    static readonly string[] EmulatorWords =
    [
        "qemu", "android emulator", "bluestacks", "ldplayer", "nox", "memu", "mumu",
        "play games", "hd-player", "dnplayer",
    ];

    static readonly HashSet<string> EmulatorProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "emulator", "HD-Player", "dnplayer", "Nox", "NoxVMHandle", "MEmu", "MEmuHeadless",
        "MuMuPlayer", "MuMuNxMain", "MuMuNxDevice", "NemuPlayer", "Bluestacks", "BlueStacksApp",
        "GooglePlayGames", "crosvm",
    };

    public static bool IsEmulatorProcess(string processName) =>
        EmulatorProcesses.Contains(processName)
        || processName.StartsWith("qemu-system", StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowButton(string name)
    {
        var norm = Normalize(name);
        if (norm.Contains("dont allow", StringComparison.Ordinal) || norm.Contains("do not allow", StringComparison.Ordinal))
            return false;
        return AllowButtons.Contains(norm);
    }

    public static bool IsBlockedProcess(string processName) =>
        processName.Equals("consent", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("CredentialUIBroker", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("smartscreen", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("WifiEmulatorAccept", StringComparison.OrdinalIgnoreCase);

    public static bool MentionsEmulator(string processName, string windowText, string extraTitles) =>
        MatchDetail(processName, windowText, extraTitles) != null;

    public static string? MatchDetail(string processName, string windowText, string extraTitles)
    {
        if (processName.Length == 0 || IsBlockedProcess(processName))
            return null;
        if (IsEmulatorProcess(processName))
            return "Windows program " + processName;

        var haystack = windowText.ToLowerInvariant();
        foreach (var word in EmulatorWords)
        {
            if (ContainsTerm(haystack, word))
                return "program name \"" + word + "\"";
        }

        foreach (var extra in extraTitles.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (extra.Length >= 3 && ContainsTerm(haystack, extra.ToLowerInvariant()))
                return "extra title \"" + extra + "\"";
        }

        return null;
    }

    static bool ContainsTerm(string haystack, string term)
    {
        var index = 0;
        while ((index = haystack.IndexOf(term, index, StringComparison.Ordinal)) >= 0)
        {
            var before = index == 0 || !char.IsLetterOrDigit(haystack[index - 1]);
            var after = index + term.Length >= haystack.Length || !char.IsLetterOrDigit(haystack[index + term.Length]);
            if (before && after)
                return true;
            index += term.Length;
        }

        return false;
    }

    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var folded = text.Trim().ToLowerInvariant()
            .Replace('\u2019', '\'')
            .Replace("'", "", StringComparison.Ordinal);
        var sb = new System.Text.StringBuilder(folded.Length);
        var pendingSpace = false;
        foreach (var ch in folded)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingSpace && sb.Length > 0)
                    sb.Append(' ');
                pendingSpace = false;
                sb.Append(ch);
            }
            else if (char.IsWhiteSpace(ch))
            {
                pendingSpace = true;
            }
        }

        return sb.ToString().Trim();
    }
}

public static class WindowsPromptAccepter
{
    public const string NoPromptMessage = "No Allow or Accept button is open.";
    public static Task<WindowsPromptResult> TryAcceptAsync(string extraTitles, CancellationToken ct)
    {
        var done = new TaskCompletionSource<WindowsPromptResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                done.SetResult(TryAccept(extraTitles));
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return done.Task;
    }

    public static WindowsPromptResult TryAccept(string extraTitles)
    {
        var messages = new List<string>();
        AutomationElement root;
        try
        {
            root = AutomationElement.RootElement;
        }
        catch (Exception ex)
        {
            messages.Add($"Windows UI Automation is unavailable: {ex.Message}");
            return new WindowsPromptResult(false, "", messages);
        }

        AutomationElementCollection windows;
        try
        {
            windows = root.FindAll(TreeScope.Children, Condition.TrueCondition);
        }
        catch (Exception ex)
        {
            messages.Add($"Could not list Windows windows: {ex.Message}");
            return new WindowsPromptResult(false, "", messages);
        }

        foreach (AutomationElement window in windows)
        {
            string name;
            int pid;
            try
            {
                name = window.Current.Name ?? "";
                pid = window.Current.ProcessId;
            }
            catch (ElementNotAvailableException)
            {
                continue;
            }

            var process = ProcessName(pid);
            if (!MightBePrompt(name, process))
                continue;

            var text = name + " " + DescendantText(window);
            var detail = WindowsPromptMatch.MatchDetail(process, text, extraTitles);
            if (detail == null)
                continue;

            var button = FindAllowButton(window);
            var who = process.Length == 0 ? $"\"{name}\"" : $"\"{name}\" ({process})";
            if (button == null)
            {
                messages.Add($"{who} matches {detail} and has no Allow button.");
                continue;
            }

            var label = button.Current.Name ?? "Allow";
            try
            {
                if (button.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern) && pattern is InvokePattern invoke)
                {
                    invoke.Invoke();
                    messages.Add($"Clicked \"{label}\" on {who}.");
                    return new WindowsPromptResult(true, label, messages);
                }
            }
            catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException)
            {
                messages.Add($"Could not click \"{label}\" on \"{name}\": {ex.Message}");
            }
        }

        if (messages.Count == 0)
            messages.Add(NoPromptMessage);
        return new WindowsPromptResult(false, "", messages);
    }

    static bool MightBePrompt(string name, string process)
    {
        if (WindowsPromptMatch.IsBlockedProcess(process))
            return false;
        if (WindowsPromptMatch.IsEmulatorProcess(process))
            return true;
        return name.Contains("Security", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Firewall", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Allow", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Android Emulator", StringComparison.OrdinalIgnoreCase)
            || name.Contains("BlueStacks", StringComparison.OrdinalIgnoreCase)
            || name.Contains("LDPlayer", StringComparison.OrdinalIgnoreCase)
            || name.Contains("MuMu", StringComparison.OrdinalIgnoreCase)
            || name.Contains("MEmu", StringComparison.OrdinalIgnoreCase)
            || name.Contains("QEMU", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Play Games", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Nox", StringComparison.OrdinalIgnoreCase);
    }

    static AutomationElement? FindAllowButton(AutomationElement window)
    {
        AutomationElementCollection buttons;
        try
        {
            buttons = window.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }

        AutomationElement? best = null;
        var bestRank = int.MaxValue;
        foreach (AutomationElement button in buttons)
        {
            string name;
            try { name = button.Current.Name ?? ""; }
            catch (ElementNotAvailableException) { continue; }
            if (!WindowsPromptMatch.IsAllowButton(name))
                continue;
            var rank = WindowsPromptMatch.Normalize(name) is "allow access" or "允许访问" ? 0 : 1;
            if (rank < bestRank)
            {
                best = button;
                bestRank = rank;
            }
        }

        return best;
    }

    static string DescendantText(AutomationElement window)
    {
        try
        {
            var texts = window.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));
            var parts = new List<string>(texts.Count);
            foreach (AutomationElement text in texts)
            {
                try
                {
                    var name = text.Current.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        parts.Add(name);
                }
                catch (ElementNotAvailableException)
                {
                    // The prompt closed while it was being read.
                }
            }

            return string.Join(' ', parts);
        }
        catch (ElementNotAvailableException)
        {
            return "";
        }
    }

    static string ProcessName(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return "";
        }
    }
}
