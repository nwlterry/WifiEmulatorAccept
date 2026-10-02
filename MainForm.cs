namespace WifiEmulatorAccept;

public sealed class MainForm : Form
{
    readonly AppSettings _settings;
    readonly CheckBox _enabled = new() { AutoSize = true, Text = "Watch Wi-Fi and auto-accept emulator prompts" };
    readonly CheckBox _startup = new() { AutoSize = true, Text = "Start when I sign in to Windows" };
    readonly Label _status = new()
    {
        AutoSize = true,
        Text = "Wi-Fi: checking...",
    };
    readonly TextBox _titles = new();
    readonly Button _scan = new() { Text = "Scan now", AutoSize = true };
    readonly Button _openLog = new() { Text = "Open log folder", AutoSize = true };
    readonly Button _help = new() { Text = "Help", AutoSize = true };
    readonly TextBox _log = new()
    {
        Multiline = true,
        ReadOnly = true,
        WordWrap = true,
        ScrollBars = ScrollBars.Vertical,
        BorderStyle = BorderStyle.None,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 9f),
    };
    readonly NotifyIcon _tray;
    string? _lastLoggedStatus;
    bool? _iconConnected;
    readonly WifiMonitor _wifi;
    readonly AcceptService _service;
    readonly System.Windows.Forms.Timer _statusTimer;
    bool _exit;
    bool _toldAboutTray;

    public MainForm(bool startInTray)
    {
        _settings = SettingsStore.Load();
        _settings.RunAtStartup = StartupManager.IsEnabled();
        var wifi = WifiStatus.Read();
        _tray = new NotifyIcon();
        UseWifiIcon(wifi.Connected);
        _tray.Text = Tip(wifi.Text);
        _tray.Visible = true;
        _service = new AcceptService(_settings, label =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => _tray.ShowBalloonTip(2500, "Emulator accepted", label, ToolTipIcon.Info));
                return;
            }
            _tray.ShowBalloonTip(2500, "Emulator accepted", label, ToolTipIcon.Info);
        });
        Text = $"Wi-Fi Emulator Accept (.NET {Environment.Version.Major})";
        ClientSize = new Size(760, 900);
        MinimumSize = new Size(680, 780);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.Font;

        var intro = new Label
        {
            AutoSize = true,
            Dock = DockStyle.None,
            Margin = new Padding(0, 0, 0, 6),
            Text = "Pure .NET program for Windows only. When Wi-Fi reconnects, this program clicks Allow or Accept on the emulator prompt (QEMU, Android Emulator, BlueStacks, LDPlayer, Nox, MEmu, MuMu, Google Play Games).",
        };
        var runtime = new Label
        {
            AutoSize = true,
            Dock = DockStyle.None,
            Margin = new Padding(0, 0, 0, 6),
            Text = AppLog.PlatformLine,
        };
        _enabled.AutoSize = true;
        _enabled.Dock = DockStyle.None;
        _enabled.Margin = new Padding(0, 4, 0, 0);
        _startup.AutoSize = true;
        _startup.Dock = DockStyle.None;
        _startup.Margin = new Padding(0, 2, 0, 4);
        _status.AutoSize = true;
        _status.Dock = DockStyle.None;
        _status.Margin = new Padding(0, 8, 0, 8);
        _status.Font = new Font(Font, FontStyle.Bold);
        var titleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.None,
            Margin = new Padding(0, 4, 0, 4),
            Text = "Extra window titles, separated by commas:",
        };
        _titles.Dock = DockStyle.None;
        _titles.Margin = new Padding(0, 0, 0, 8);
        _titles.PlaceholderText = "Example: BlueStacks, LDPlayer, Nox";
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.None,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 4),
        };
        _scan.Margin = new Padding(0, 0, 8, 0);
        _openLog.Margin = new Padding(0, 0, 8, 0);
        buttons.Controls.Add(_scan);
        buttons.Controls.Add(_openLog);
        buttons.Controls.Add(_help);

        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 12, 16, 8),
        };
        header.Controls.Add(intro);
        header.Controls.Add(runtime);
        header.Controls.Add(_enabled);
        header.Controls.Add(_startup);
        header.Controls.Add(_status);
        header.Controls.Add(titleLabel);
        header.Controls.Add(_titles);
        header.Controls.Add(buttons);

        var logFrame = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 0, 16, 12),
        };
        var logBorder = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6),
            BorderStyle = BorderStyle.FixedSingle,
        };
        logBorder.Controls.Add(_log);
        logFrame.Controls.Add(logBorder);
        Controls.Add(logFrame);
        Controls.Add(header);
        header.SizeChanged += (_, _) =>
        {
            var width = Math.Max(240, header.ClientSize.Width - header.Padding.Horizontal);
            if (intro.Width == width && intro.MaximumSize.Width == width)
                return;
            intro.MaximumSize = new Size(width, 0);
            runtime.MaximumSize = new Size(width, 0);
            _status.MaximumSize = new Size(width, 0);
            titleLabel.MaximumSize = new Size(width, 0);
            intro.Width = width;
            runtime.Width = width;
            _enabled.Width = width;
            _startup.Width = width;
            _status.Width = width;
            titleLabel.Width = width;
            _titles.Width = width;
            buttons.Width = width;
        };

        _enabled.Checked = _settings.Enabled;
        _startup.Checked = _settings.RunAtStartup;
        _titles.Text = _settings.ExtraTitleContains;

        _enabled.CheckedChanged += (_, _) =>
        {
            _settings.Enabled = _enabled.Checked;
            Persist();
            SetStatus(WifiStatus.Read());
        };
        _startup.CheckedChanged += (_, _) =>
        {
            _settings.RunAtStartup = _startup.Checked;
            try
            {
                StartupManager.Apply(_settings.RunAtStartup);
                Persist();
                SetStatus(WifiStatus.Read());
            }
            catch (Exception ex)
            {
                AppLog.Write($"Could not change startup: {ex.Message} {WifiStatus.Read().Text}.");
            }
        };
        _titles.Leave += (_, _) =>
        {
            var next = _titles.Text.Trim();
            if (next == _settings.ExtraTitleContains)
                return;
            _settings.ExtraTitleContains = next;
            Persist();
            SetStatus(WifiStatus.Read());
        };
        _scan.Click += async (_, _) =>
        {
            _settings.ExtraTitleContains = _titles.Text.Trim();
            Persist();
            _scan.Enabled = false;
            try
            {
                await _service.ScanOnceInteractiveAsync();
            }
            catch (Exception ex)
            {
                AppLog.Write($"Scan failed: {ex.Message} {WifiStatus.Read().Text}.");
            }
            finally
            {
                _scan.Enabled = true;
            }
        };
        _openLog.Click += (_, _) =>
        {
            Directory.CreateDirectory(SettingsStore.Dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = SettingsStore.Dir,
                UseShellExecute = true,
            });
        };
        _help.Click += (_, _) => OpenHelp();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => ShowFromTray());
        menu.Items.Add("Scan now", null, async (_, _) =>
        {
            try { await _service.ScanOnceInteractiveAsync(); }
            catch (Exception ex) { AppLog.Write($"Scan failed: {ex.Message} {WifiStatus.Read().Text}."); }
        });
        menu.Items.Add("Help", null, (_, _) => OpenHelp());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _exit = true;
            Close();
        });
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowFromTray();

        AppLog.Written += line => AppendLog(line);
        AppLog.ReplaceStaleLog();
        LoadLog();

        _wifi = new WifiMonitor();
        _wifi.Reconnected += (_, _) => _service.OnReconnected();
        _wifi.StatusChanged += (_, state) => SetStatus(state);
        SetStatus(wifi);

        _statusTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _statusTimer.Tick += (_, _) => SetStatus(WifiStatus.Read());
        _statusTimer.Start();

        if (startInTray)
        {
            Shown += (_, _) =>
            {
                Hide();
                AppLog.Write("Started in the tray. " + AppLog.Describe(_settings, WifiStatus.Read()));
            };
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F1)
        {
            OpenHelp();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    static void OpenHelp()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Help.html");
        if (!File.Exists(path))
        {
            MessageBox.Show(
                "Help.html was not found next to the program.",
                "Wi-Fi Emulator Accept",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    void SetStatus(WifiState state)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(state));
            return;
        }
        _status.Text = AppLog.StatusText(_settings, state);
        var described = AppLog.Describe(_settings, state);
        _tray.Text = Tip(described);
        UseWifiIcon(state.Connected);
        LogStatus(state);
    }

    void LogStatus(WifiState state)
    {
        var line = AppLog.Describe(_settings, state);
        if (line == _lastLoggedStatus)
            return;
        _lastLoggedStatus = line;
        AppLog.Write(line);
    }

    static string Tip(string text) => text.Length <= 127 ? text : text[..127];

    void LoadLog()
    {
        try
        {
            var path = Path.Combine(SettingsStore.Dir, "log.txt");
            if (!File.Exists(path))
                return;
            var text = AppLog.WithoutStaleLines(File.ReadAllText(path));
            if (text.Length == 0)
            {
                _log.Clear();
                return;
            }
            if (text.Length > 30000)
                text = text[^30000..];
            _log.Text = text + Environment.NewLine;
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }
        catch
        {
            // The next status line still appears on screen.
        }
    }

    void UseWifiIcon(bool connected)
    {
        if (_iconConnected == connected)
            return;
        _iconConnected = connected;
        var size = Math.Max(32, SystemInformation.SmallIconSize.Width * 2);
        var nextTray = WifiIcons.Tile(connected, size);
        var nextWindow = WifiIcons.Tile(connected, 32);
        var previousTray = _tray.Icon;
        var previousWindow = Icon;
        _tray.Icon = nextTray;
        Icon = nextWindow;
        previousTray?.Dispose();
        if (previousWindow != null && !ReferenceEquals(previousWindow, nextWindow))
            previousWindow.Dispose();
    }

    void Persist() => SettingsStore.Save(_settings);

    void AppendLog(string line)
    {
        if (IsDisposed)
            return;
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(line));
            return;
        }

        if (AppLog.IsStale(line))
            return;
        _log.AppendText(line + Environment.NewLine);
        if (_log.TextLength > 50000)
            _log.Text = _log.Text[^30000..];
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            if (!_toldAboutTray)
            {
                _toldAboutTray = true;
                _tray.ShowBalloonTip(2500, "Wi-Fi Emulator Accept", "Still watching in the tray. Right-click the icon and choose Exit to quit.", ToolTipIcon.Info);
                AppLog.Write("The window is hidden. Watching continues in the tray. " + AppLog.Describe(_settings, WifiStatus.Read()));
            }
            return;
        }

        AppLog.Write("Stopped. " + AppLog.Describe(_settings, WifiStatus.Read()));
        _statusTimer.Stop();
        _wifi.Dispose();
        _service.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        base.OnFormClosing(e);
    }
}
