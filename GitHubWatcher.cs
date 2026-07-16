using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StatusNeko;

public sealed class GitHubWatcher : IDisposable
{
    private const string EventsUrl = "https://api.github.com/users/{0}/events/public";

    private readonly HttpClient _http = new();
    private System.Threading.Timer? _timer;
    private string _username = "";
    private string _token = "";

    public event Action<GitHubEventInfo>? OnUpdate;
    public event Action? OnError;

    public bool HasCredentials => !string.IsNullOrEmpty(_username);

    public void SetCredentials(string username, string token = "")
    {
        _username = username;
        _token = token;
    }

    public void Start()
    {
        _timer = new System.Threading.Timer(async _ => await FetchAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
    }

    public void Refresh()
    {
        _ = FetchAsync();
    }

    private async Task FetchAsync()
    {
        if (!HasCredentials)
        {
            OnError?.Invoke();
            return;
        }

        try
        {
            var pushEvent = await FindLatestPushEventAsync();
            if (pushEvent == null)
            {
                OnUpdate?.Invoke(new GitHubEventInfo("", "", DateTime.MinValue));
                return;
            }

            var repoName = pushEvent.Repo?.Name ?? "";
            var headSha = pushEvent.HeadSha ?? "";

            if (!string.IsNullOrEmpty(repoName) && !string.IsNullOrEmpty(headSha))
            {
                var commit = await FetchCommitAsync(repoName, headSha);
                if (commit != null && !string.IsNullOrEmpty(commit.Message))
                {
                    OnUpdate?.Invoke(new GitHubEventInfo(
                        Repo: repoName,
                        Message: commit.Message,
                        Timestamp: commit.Timestamp
                    ));
                    return;
                }
            }

            OnUpdate?.Invoke(new GitHubEventInfo(repoName, "pushed", DateTime.MinValue));
        }
        catch
        {
            OnError?.Invoke();
        }
    }

    private async Task<GitHubEvent?> FindLatestPushEventAsync()
    {
        var url = string.Format(EventsUrl, _username);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyHeaders(request);

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var events = await response.Content.ReadFromJsonAsync<List<GitHubEvent>>();
        return events?.FirstOrDefault(e => e?.Type == "PushEvent" && !string.IsNullOrEmpty(e.HeadSha));
    }

    private async Task<GitHubCommitDetail?> FetchCommitAsync(string repo, string sha)
    {
        var url = $"https://api.github.com/repos/{repo}/commits/{sha}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyHeaders(request);

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GitHubCommitDetail>();
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        request.Headers.UserAgent.ParseAdd("StatusNeko/1.0");
        if (!string.IsNullOrEmpty(_token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
    }

    public void Dispose()
    {
        Stop();
        _http.Dispose();
    }

    // ── event list response ──

    private record GitHubEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("repo")]
        public GitHubRepo? Repo { get; init; }

        [JsonPropertyName("payload")]
        public GitHubPushPayload? Payload { get; init; }

        public string? HeadSha => Payload?.Head;
    }

    private record GitHubRepo
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private record GitHubPushPayload
    {
        [JsonPropertyName("head")]
        public string? Head { get; init; }
    }

    // ── single commit response ──

    private record GitHubCommitDetail
    {
        [JsonPropertyName("commit")]
        public GitHubCommitInfo? Commit { get; init; }

        public string? Message => Commit?.Message;
        public DateTime Timestamp => Commit?.Author?.Date ?? DateTime.MinValue;
    }

    private record GitHubCommitInfo
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("author")]
        public GitHubAuthor? Author { get; init; }
    }

    private record GitHubAuthor
    {
        [JsonPropertyName("date")]
        public DateTime? Date { get; init; }
    }
}
