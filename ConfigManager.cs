using System.Text.Json;
using System.Text.Json.Serialization;

namespace StatusNeko;

public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "config.json"
    );

    public static Config Load()
    {
        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }
        catch
        {
            return new Config();
        }
    }

    public static void Save(Config config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigPath, json);
    }
}

public record Config
{
    [JsonPropertyName("steam_api_key")]
    public string SteamApiKey { get; init; } = "";

    [JsonPropertyName("steam_steam_id")]
    public string SteamSteamId { get; init; } = "";

    [JsonPropertyName("github_username")]
    public string GitHubUsername { get; init; } = "";

    [JsonPropertyName("github_token")]
    public string GitHubToken { get; init; } = "";

    [JsonPropertyName("api_push_url")]
    public string ApiPushUrl { get; init; } = "";
}
