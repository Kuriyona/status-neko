using System.Drawing.Drawing2D;
using System.Text.Json;

namespace StatusNeko;

public sealed class MainForm : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly SMTCMonitor _monitor = new();
    private readonly SteamWatcher _steamWatcher = new();
    private readonly HttpClient _apiClient = new();

    private readonly Label _sourceLabel;
    private readonly Label _titleLabel;
    private readonly Label _artistLabel;
    private readonly Label _personaLabel;
    private readonly Label _steamLabel;

    private MediaInfo? _currentMedia;
    private SteamInfo? _currentSteam;
    private string _apiPushUrl = "";

    public MainForm()
    {
        Text = "Status Neko";
        ClientSize = new Size(450, 220);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;

        var refreshBtn = new Button
        {
            Text = "刷新",
            Font = new Font("Segoe UI", 9),
            Location = new Point(218, 6),
            Size = new Size(50, 24)
        };
        refreshBtn.Click += (_, _) => _steamWatcher.Refresh();

        var pushBtn = new Button
        {
            Text = "推送",
            Font = new Font("Segoe UI", 9),
            Location = new Point(272, 6),
            Size = new Size(50, 24)
        };
        pushBtn.Click += (_, _) => _ = TryPushToApiAsync();

        var settingsBtn = new Button
        {
            Text = "设置",
            Font = new Font("Segoe UI", 9),
            Location = new Point(326, 6),
            Size = new Size(50, 24)
        };
        settingsBtn.Click += (_, _) => OpenSettings();

        var deleteBtn = new Button
        {
            Text = "删除",
            Font = new Font("Segoe UI", 9),
            Location = new Point(380, 6),
            Size = new Size(50, 24)
        };
        deleteBtn.Click += (_, _) => _ = TryDeleteAsync();

        // ── Section 1: SMTC ──

        var sec1 = new Label
        {
            Location = new Point(16, 34),
            Size = new Size(418, 18),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 51, 51),
            Text = "SMTC 信息"
        };

        _sourceLabel = new Label
        {
            Location = new Point(16, 54),
            Size = new Size(418, 16),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            Text = ""
        };

        _titleLabel = new Label
        {
            Location = new Point(16, 72),
            Size = new Size(418, 24),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Text = "(no media)"
        };

        _artistLabel = new Label
        {
            Location = new Point(16, 96),
            Size = new Size(418, 20),
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(68, 68, 68),
            Text = ""
        };

        var sep1 = new Label
        {
            Location = new Point(16, 120),
            Size = new Size(418, 1),
            BackColor = Color.FromArgb(221, 221, 221),
            BorderStyle = BorderStyle.None
        };

        // ── Section 2: Steam ──

        var sec2 = new Label
        {
            Location = new Point(16, 126),
            Size = new Size(418, 18),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 51, 51),
            Text = "Steam 状态"
        };

        _personaLabel = new Label
        {
            Location = new Point(16, 146),
            Size = new Size(418, 18),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            Text = "Steam: not configured"
        };

        _steamLabel = new Label
        {
            Location = new Point(16, 166),
            Size = new Size(280, 22),
            Font = new Font("Segoe UI", 10),
            Text = ""
        };

        var sep2 = new Label
        {
            Location = new Point(16, 192),
            Size = new Size(418, 1),
            BackColor = Color.FromArgb(221, 221, 221),
            BorderStyle = BorderStyle.None
        };

        Controls.AddRange(new Control[] {
            refreshBtn, pushBtn, settingsBtn, deleteBtn,
            sec1, _sourceLabel, _titleLabel, _artistLabel, sep1,
            sec2, _personaLabel, _steamLabel, sep2
        });

        // ── Tray icon ──

        _trayIcon = new NotifyIcon
        {
            Icon = CreatePlaceholderIcon(),
            Text = "Status Neko",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("显示窗口", null, (_, _) => ShowWindow());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("设置", null, (_, _) => OpenSettings());
        contextMenu.Items.Add("刷新", null, (_, _) => _steamWatcher.Refresh());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("退出", null, (_, _) => Quit());
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        };

        // ── Event wiring ──

        _monitor.OnMediaUpdate += info =>
        {
            BeginInvoke(() => OnMediaUpdate(info));
        };
        _monitor.OnMediaStopped += () =>
        {
            BeginInvoke(() => OnMediaStopped());
        };

        _steamWatcher.OnUpdate += info =>
        {
            BeginInvoke(() => OnSteamUpdate(info));
        };
        _steamWatcher.OnError += () =>
        {
            BeginInvoke(() => OnSteamError());
        };

        LoadConfig();
        _steamWatcher.Start();
        Load += (_, _) => _ = _monitor.StartAsync();
    }

    private void LoadConfig()
    {
        var cfg = ConfigManager.Load();
        _apiPushUrl = cfg.ApiPushUrl;

        if (!string.IsNullOrEmpty(cfg.SteamApiKey) && !string.IsNullOrEmpty(cfg.SteamSteamId))
        {
            _steamWatcher.SetCredentials(cfg.SteamApiKey, cfg.SteamSteamId);
            _steamWatcher.Refresh();
            _personaLabel.Text = "Steam: loading...";
        }
    }

    private void OnMediaUpdate(MediaInfo info)
    {
        _currentMedia = info;
        _sourceLabel.Text = info.SourceApp;
        _titleLabel.Text = string.IsNullOrEmpty(info.Title) ? "(no title)" : info.Title;
        _artistLabel.Text = info.Artist;
        UpdateTooltip();
        _ = TryPushToApiAsync();
    }

    private void OnMediaStopped()
    {
        _currentMedia = null;
        _sourceLabel.Text = "";
        _titleLabel.Text = "(no media)";
        _artistLabel.Text = "";
        UpdateTooltip();
        _ = TryPushToApiAsync();
    }

    private void OnSteamUpdate(SteamInfo info)
    {
        _currentSteam = info;

        var name = info.PersonaName;
        var status = PersonaStateText(info.PersonaState);
        _personaLabel.Text = string.IsNullOrEmpty(name) ? "Steam: unknown" : $"{name}  —  {status}";

        _steamLabel.Text = !string.IsNullOrEmpty(info.Game)
            ? info.Game
            : "Steam: not playing";

        UpdateTooltip();
        _ = TryPushToApiAsync();
    }

    private void OnSteamError()
    {
        _currentSteam = null;
        _personaLabel.Text = "Steam: error";
        _steamLabel.Text = "";
        UpdateTooltip();
        _ = TryPushToApiAsync();
    }

    private void UpdateTooltip()
    {
        var lines = new List<string> { "Status Neko" };

        if (_currentMedia != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(_currentMedia.Title))
                parts.Add(_currentMedia.Title);
            if (!string.IsNullOrEmpty(_currentMedia.Artist))
                parts.Add(_currentMedia.Artist);
            if (!string.IsNullOrEmpty(_currentMedia.Album))
                parts.Add($"[{_currentMedia.Album}]");
            if (parts.Count > 0)
                lines.Add(string.Join(" - ", parts));
        }

        if (_currentSteam != null)
        {
            if (!string.IsNullOrEmpty(_currentSteam.PersonaName))
                lines.Add($"{_currentSteam.PersonaName}  —  {PersonaStateText(_currentSteam.PersonaState)}");
            if (_currentSteam.Game.Length > 0)
                lines.Add(_currentSteam.Game);
        }

        var text = string.Join("\n", lines);
        if (text.Length > 127)
            text = text[..124] + "...";
        _trayIcon.Text = text;
    }

    private async Task TryPushToApiAsync()
    {
        if (string.IsNullOrEmpty(_apiPushUrl)) return;

        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["media"] = _currentMedia != null ? new
                {
                    title = _currentMedia.Title,
                    artist = _currentMedia.Artist,
                    album = _currentMedia.Album,
                    source_app = _currentMedia.SourceApp
                } : null,
                ["steam"] = _currentSteam != null ? new
                {
                    persona_name = _currentSteam.PersonaName,
                    state = _currentSteam.PersonaState,
                    game = _currentSteam.Game
                } : null
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _apiClient.PostAsync(_apiPushUrl, content);
        }
        catch
        {
        }
    }

    private async Task TryDeleteAsync()
    {
        if (string.IsNullOrEmpty(_apiPushUrl)) return;

        try
        {
            await _apiClient.DeleteAsync(_apiPushUrl);
        }
        catch
        {
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _ = TryDeleteAsync();
        base.OnFormClosing(e);
    }

    private static string PersonaStateText(int state) => state switch
    {
        0 => "Offline",
        1 => "Online",
        2 => "Busy",
        3 => "Away",
        4 => "Snooze",
        5 => "Looking to trade",
        6 => "Looking to play",
        _ => "Unknown"
    };

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    private void OpenSettings()
    {
        var cfg = ConfigManager.Load();
        using var form = new SettingsForm(_steamWatcher);
        form.LoadCredentials(cfg.SteamApiKey, cfg.SteamSteamId, cfg.ApiPushUrl);
        form.ShowDialog(this);
        _apiPushUrl = ConfigManager.Load().ApiPushUrl;
    }

    private void Quit()
    {
        _monitor.Stop();
        _steamWatcher.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    private static Icon CreatePlaceholderIcon()
    {
        var size = 64;
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var brush = new SolidBrush(Color.FromArgb(100, 180, 255));
        g.FillEllipse(brush, size * 0.1f, size * 0.1f, size * 0.8f, size * 0.8f);

        var hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitor.Stop();
            _steamWatcher.Stop();
            _steamWatcher.Dispose();
            _trayIcon.Dispose();
            _apiClient.Dispose();
        }
        base.Dispose(disposing);
    }
}
