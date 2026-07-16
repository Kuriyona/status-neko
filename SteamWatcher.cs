using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StatusNeko;

public sealed class SteamWatcher : IDisposable
{
    private const string ApiUrl = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private System.Threading.Timer? _timer;
    private string _apiKey = "";
    private string _steamId = "";

    public event Action<SteamInfo>? OnUpdate;
    public event Action? OnError;

    public bool HasCredentials => !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_steamId);

    public void SetCredentials(string apiKey, string steamId)
    {
        _apiKey = apiKey;
        _steamId = steamId;
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
            var url = $"{ApiUrl}?key={_apiKey}&steamids={_steamId}";
            var response = await _http.GetFromJsonAsync<SteamResponse>(url);

            var player = response?.Response?.Players?.FirstOrDefault();

            OnUpdate?.Invoke(new SteamInfo(
                Game: player?.GameExtraInfo ?? "",
                GameId: player?.GameId ?? "",
                PersonaName: player?.PersonaName ?? "",
                PersonaState: player?.PersonaState ?? 0,
                RealName: player?.RealName ?? ""
            ));
        }
        catch
        {
            OnError?.Invoke();
        }
    }

    public void Dispose()
    {
        Stop();
        _http.Dispose();
    }

    private record SteamResponse
    {
        [JsonPropertyName("response")]
        public SteamResponseData? Response { get; init; }
    }

    private record SteamResponseData
    {
        [JsonPropertyName("players")]
        public List<SteamPlayer>? Players { get; init; }
    }

    private record SteamPlayer
    {
        [JsonPropertyName("gameextrainfo")]
        public string? GameExtraInfo { get; init; }

        [JsonPropertyName("gameid")]
        public string? GameId { get; init; }

        [JsonPropertyName("personaname")]
        public string? PersonaName { get; init; }

        [JsonPropertyName("personastate")]
        public int? PersonaState { get; init; }

        [JsonPropertyName("realname")]
        public string? RealName { get; init; }
    }
}
