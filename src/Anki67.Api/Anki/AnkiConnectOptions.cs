namespace Anki67.Api.Anki;

public sealed class AnkiConnectOptions
{
    public const string SectionName = "AnkiConnect";

    public string Endpoint { get; set; } = "http://127.0.0.1:8765";

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 15;
}
