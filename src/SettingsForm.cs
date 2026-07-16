namespace StatusNeko;

public sealed class SettingsForm : Form
{
    private readonly TextBox _apiKeyBox;
    private readonly TextBox _steamIdBox;
    private readonly TextBox _usernameBox;
    private readonly TextBox _tokenBox;
    private readonly TextBox _apiUrlBox;
    private readonly SteamWatcher _steamWatcher;
    private readonly GitHubWatcher _gitHubWatcher;

    public SettingsForm(SteamWatcher steamWatcher, GitHubWatcher gitHubWatcher)
    {
        _steamWatcher = steamWatcher;
        _gitHubWatcher = gitHubWatcher;

        Text = "设置";
        ClientSize = new Size(380, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var y = 14;
        var gap = 22;

        AddLabel("Steam API Key:", 16, y);
        _apiKeyBox = AddTextBox(16, y += gap);

        y += gap + 8;
        AddLabel("Steam ID:", 16, y);
        _steamIdBox = AddTextBox(16, y += gap);

        y += gap + 8;
        AddLabel("GitHub 用户名:", 16, y);
        _usernameBox = AddTextBox(16, y += gap);

        y += gap + 8;
        AddLabel("GitHub Token (可选):", 16, y);
        _tokenBox = AddTextBox(16, y += gap);

        y += gap + 8;
        AddLabel("API 推送 URL (可选):", 16, y);
        _apiUrlBox = AddTextBox(16, y += gap);

        var saveBtn = new Button
        {
            Text = "保存",
            Location = new Point(204, 270),
            Size = new Size(80, 28)
        };
        saveBtn.Click += Save;

        var cancelBtn = new Button
        {
            Text = "取消",
            Location = new Point(290, 270),
            Size = new Size(80, 28)
        };
        cancelBtn.Click += (_, _) => Close();

        Controls.AddRange(new Control[] {
            _apiKeyBox, _steamIdBox, _usernameBox, _tokenBox, _apiUrlBox,
            saveBtn, cancelBtn
        });
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(120, 20)
        });
    }

    private static TextBox AddTextBox(int x, int y)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(348, 24)
        };
    }

    public void LoadCredentials(string apiKey, string steamId, string gitHubUser, string gitHubToken, string apiPushUrl)
    {
        _apiKeyBox.Text = apiKey;
        _steamIdBox.Text = steamId;
        _usernameBox.Text = gitHubUser;
        _tokenBox.Text = gitHubToken;
        _apiUrlBox.Text = apiPushUrl;
    }

    private void Save(object? sender, EventArgs e)
    {
        var key = _apiKeyBox.Text.Trim();
        var sid = _steamIdBox.Text.Trim();
        var ghUser = _usernameBox.Text.Trim();
        var ghToken = _tokenBox.Text.Trim();
        var apiUrl = _apiUrlBox.Text.Trim();

        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(sid))
        {
            _steamWatcher.SetCredentials(key, sid);
            _steamWatcher.Refresh();
        }

        _gitHubWatcher.SetCredentials(ghUser, ghToken);
        if (!string.IsNullOrEmpty(ghUser))
            _gitHubWatcher.Refresh();

        ConfigManager.Save(new Config
        {
            SteamApiKey = key,
            SteamSteamId = sid,
            GitHubUsername = ghUser,
            GitHubToken = ghToken,
            ApiPushUrl = apiUrl
        });

        Close();
    }
}
