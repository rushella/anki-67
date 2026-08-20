namespace Anki67.Web.Anki;

public sealed record AnkiConnectionStatus(
    bool Connected,
    int? Version,
    string? Profile,
    int DeckCount,
    string Endpoint,
    string Message);

public sealed record AnkiField(string Value, int Order);

public sealed record AnkiNote(
    long NoteId,
    string Profile,
    string ModelName,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, AnkiField> Fields,
    long ModifiedAt,
    IReadOnlyList<long> Cards);

public sealed record AnkiNoteSearchResult(
    IReadOnlyList<AnkiNote> Notes,
    int Total,
    bool Truncated);

public sealed record UpdateAnkiNote(
    IReadOnlyDictionary<string, string> Fields,
    IReadOnlyList<string> Tags);

public sealed record AnkiSyncResult(DateTimeOffset SyncedAt, string Message);
