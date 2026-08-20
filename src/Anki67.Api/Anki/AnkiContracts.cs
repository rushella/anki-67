namespace Anki67.Api.Anki;

public sealed record AnkiConnectionStatusResponse(
    bool Connected,
    int? Version,
    string? Profile,
    int DeckCount,
    string Endpoint,
    string Message);

public sealed record AnkiFieldResponse(string Value, int Order);

public sealed record AnkiNoteResponse(
    long NoteId,
    string Profile,
    string ModelName,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, AnkiFieldResponse> Fields,
    long ModifiedAt,
    IReadOnlyList<long> Cards);

public sealed record AnkiConnectNoteInfo(
    long NoteId,
    string Profile,
    string ModelName,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, AnkiFieldResponse> Fields,
    long Mod,
    IReadOnlyList<long> Cards);

public sealed record AnkiNoteSearchResponse(
    IReadOnlyList<AnkiNoteResponse> Notes,
    int Total,
    bool Truncated);

public sealed record UpdateAnkiNoteRequest(
    Dictionary<string, string>? Fields,
    List<string>? Tags);

public sealed record AnkiSyncResponse(DateTimeOffset SyncedAt, string Message);
