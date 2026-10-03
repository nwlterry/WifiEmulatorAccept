namespace WifiEmulatorAccept;

public sealed class AcceptService : IDisposable
{
    const int InitialDelayMs = 2000;
    const int WatchMs = 40000;
    const int IntervalMs = 4000;
    const int MaxAccepts = 3;

    readonly AppSettings _settings;
    readonly Action<string>? _accepted;
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly CancellationTokenSource _cts = new();
    int _watchRunning;

    public AcceptService(AppSettings settings, Action<string>? accepted = null)
    {
        _settings = settings;
        _accepted = accepted;
    }

    public void OnReconnected()
    {
        if (_cts.IsCancellationRequested)
            return;
        var wifi = WifiStatus.Read();
        var where = string.IsNullOrWhiteSpace(wifi.Ssid) ? "Wi-Fi reconnected." : $"Wi-Fi reconnected to {wifi.Ssid}.";
        if (!_settings.Enabled)
        {
            AppLog.Write($"{where} Watching is paused.");
            return;
        }

        if (Interlocked.CompareExchange(ref _watchRunning, 1, 0) != 0)
        {
            AppLog.Write($"{where} A scan is already running.");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await WatchAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // App is closing.
            }
            catch (Exception ex)
            {
                AppLog.Write($"Scan failed: {ex.Message} {WifiStatus.Read().Text}.");
            }
            finally
            {
                Interlocked.Exchange(ref _watchRunning, 0);
            }
        });
    }

    public async Task ScanOnceInteractiveAsync()
    {
        AppLog.Write("Scan now. " + AppLog.Describe(_settings, WifiStatus.Read()));
        var prompt = await ScanOnceAsync(_cts.Token);
        foreach (var message in prompt.Messages)
            AppLog.Write(WithWifi(message));
        if (prompt.Clicked)
            _accepted?.Invoke(string.IsNullOrWhiteSpace(prompt.Label) ? "Allow" : prompt.Label);
    }

    async Task WatchAsync(CancellationToken ct)
    {
        var wifi = WifiStatus.Read();
        var where = string.IsNullOrWhiteSpace(wifi.Ssid) ? "Wi-Fi reconnected." : $"Wi-Fi reconnected to {wifi.Ssid}.";
        AppLog.Write($"{where} Looking for Allow or Accept. {AppLog.Describe(_settings, wifi)}");
        await Task.Delay(InitialDelayMs, ct);
        var started = Environment.TickCount64;
        var accepts = 0;
        var sawWindow = false;
        var reported = new HashSet<string>(StringComparer.Ordinal);
        while (!ct.IsCancellationRequested && _settings.Enabled && accepts < MaxAccepts)
        {
            var prompt = await ScanOnceAsync(ct);
            foreach (var message in prompt.Messages)
            {
                if (message == WindowsPromptAccepter.NoPromptMessage)
                    continue;
                if (message != "A scan is already running.")
                    sawWindow = true;
                if (reported.Add(message))
                    AppLog.Write(WithWifi(message));
            }

            if (prompt.Clicked)
            {
                accepts++;
                _accepted?.Invoke(string.IsNullOrWhiteSpace(prompt.Label) ? "Allow" : prompt.Label);
            }

            if (accepts >= MaxAccepts || Environment.TickCount64 - started >= WatchMs)
                break;
            await Task.Delay(prompt.Clicked ? 1500 : IntervalMs, ct);
        }

        var end = WifiStatus.Read();
        var accepted = accepts == 1 ? "Clicked Allow 1 time." : $"Clicked Allow {accepts} times.";
        if (!_settings.Enabled)
            AppLog.Write($"Watch stopped. Watching is paused. {accepted} {AppLog.Describe(_settings, end)}");
        else if (accepts == 0 && sawWindow)
            AppLog.Write($"Watch finished. A matching window was open and no Allow button was clicked. {AppLog.Describe(_settings, end)}");
        else if (accepts == 0)
            AppLog.Write($"Watch finished. No Allow or Accept button was open. {AppLog.Describe(_settings, end)}");
        else
            AppLog.Write($"Watch finished. {accepted} {AppLog.Describe(_settings, end)}");
    }

    async Task<WindowsPromptResult> ScanOnceAsync(CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
            return new WindowsPromptResult(false, "", ["A scan is already running."]);

        try
        {
            return await WindowsPromptAccepter.TryAcceptAsync(_settings.ExtraTitleContains, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    static string WithWifi(string message)
    {
        var wifi = WifiStatus.Read().Text;
        return message.Contains(wifi, StringComparison.Ordinal) ? message : $"{message} {wifi}.";
    }

    public void Dispose() => _cts.Cancel();
}
