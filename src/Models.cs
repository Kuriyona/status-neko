namespace StatusNeko;

public record MediaInfo(
    string Title,
    string Artist,
    string Album,
    string SourceApp
);

public record SteamInfo(
    string Game,
    string GameId,
    string PersonaName,
    int PersonaState,
    string RealName
);

