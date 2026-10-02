using System.Net.NetworkInformation;

namespace WifiEmulatorAccept;

public sealed class WifiMonitor : IDisposable
{
    public event EventHandler? Reconnected;
    public event EventHandler<WifiState>? StatusChanged;

    readonly System.Windows.Forms.Timer _timer;
    readonly long _started = Environment.TickCount64;
    long _lastRaise;
    bool _wasUp;
    string _lastText = "";
    bool _disposed;

    public WifiMonitor()
    {
        var initial = WifiStatus.Read();
        _wasUp = initial.Connected;
        _lastText = initial.Text;
        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
        _timer = new System.Windows.Forms.Timer { Interval = 2000 };
        _timer.Tick += (_, _) => Poll();
        _timer.Start();
    }

    void Poll()
    {
        WifiState state;
        try
        {
            state = WifiStatus.Read();
        }
        catch
        {
            return;
        }

        if (state.Text != _lastText)
        {
            _lastText = state.Text;
            StatusChanged?.Invoke(this, state);
        }

        if (state.Connected && !_wasUp)
            RaiseReconnect();
        _wasUp = state.Connected;
    }

    void OnNetworkChanged(object? sender, EventArgs e)
    {
        try
        {
            Poll();
        }
        catch
        {
            // Network queries can throw while an adapter is disappearing.
        }
    }

    void RaiseReconnect()
    {
        var now = Environment.TickCount64;
        if (now - _started < 8000 || now - _lastRaise < 8000)
            return;
        _lastRaise = now;
        Reconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkChanged;
        _timer.Stop();
        _timer.Dispose();
    }
}
