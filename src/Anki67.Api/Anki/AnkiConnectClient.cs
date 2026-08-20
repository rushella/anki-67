using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Anki67.Api.Anki;

public sealed class AnkiConnectClient
{
    private const int ApiVersion = 6;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public AnkiConnectClient(HttpClient httpClient, IOptions<AnkiConnectOptions> options)
    {
        _httpClient = httpClient;
        _apiKey = string.IsNullOrWhiteSpace(options.Value.ApiKey) ? null : options.Value.ApiKey;
    }

    public string Endpoint => _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;

    public Task<int> GetVersionAsync(CancellationToken cancellationToken) =>
        InvokeAsync<int>("version", null, cancellationToken);

    public Task<string> GetActiveProfileAsync(CancellationToken cancellationToken) =>
        InvokeAsync<string>("getActiveProfile", null, cancellationToken);

    public async Task<IReadOnlyList<string>> GetDeckNamesAsync(CancellationToken cancellationToken)
    {
        var decks = await InvokeAsync<string[]>("deckNames", null, cancellationToken);
        return decks.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<AnkiNoteSearchResponse> SearchNotesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var noteIds = await InvokeAsync<long[]>("findNotes", new { query }, cancellationToken);
        var selectedIds = noteIds.Take(limit).ToArray();

        if (selectedIds.Length == 0)
        {
            return new AnkiNoteSearchResponse([], noteIds.Length, false);
        }

        var notes = await InvokeAsync<AnkiConnectNoteInfo[]>(
            "notesInfo",
            new { notes = selectedIds },
            cancellationToken);

        return new AnkiNoteSearchResponse(
            notes.Select(ToResponse).ToArray(),
            noteIds.Length,
            noteIds.Length > selectedIds.Length);
    }

    public async Task<AnkiNoteResponse?> GetNoteAsync(long noteId, CancellationToken cancellationToken)
    {
        var notes = await InvokeAsync<AnkiConnectNoteInfo[]>(
            "notesInfo",
            new { notes = new[] { noteId } },
            cancellationToken);

        var note = notes.SingleOrDefault();
        return note is null ? null : ToResponse(note);
    }

    public async Task<AnkiNoteResponse?> UpdateNoteAsync(
        long noteId,
        IReadOnlyDictionary<string, string>? fields,
        IReadOnlyCollection<string>? tags,
        CancellationToken cancellationToken)
    {
        var note = new Dictionary<string, object>
        {
            ["id"] = noteId
        };

        if (fields is not null)
        {
            note["fields"] = fields;
        }

        if (tags is not null)
        {
            note["tags"] = tags;
        }

        await InvokeAsync<JsonElement>("updateNote", new { note }, cancellationToken);
        return await GetNoteAsync(noteId, cancellationToken);
    }

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        await InvokeAsync<JsonElement>("sync", null, cancellationToken);
    }

    private static AnkiNoteResponse ToResponse(AnkiConnectNoteInfo note) => new(
        note.NoteId,
        note.Profile,
        note.ModelName,
        note.Tags,
        note.Fields,
        note.Mod,
        note.Cards);

    private async Task<T> InvokeAsync<T>(
        string action,
        object? parameters,
        CancellationToken cancellationToken)
    {
        var request = new Dictionary<string, object?>
        {
            ["action"] = action,
            ["version"] = ApiVersion
        };

        if (parameters is not null)
        {
            request["params"] = parameters;
        }

        if (_apiKey is not null)
        {
            request["key"] = _apiKey;
        }

        try
        {
            var requestJson = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(string.Empty, content, cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);
            var envelope = document.RootElement;

            if (envelope.ValueKind != JsonValueKind.Object
                || !envelope.TryGetProperty("result", out var result)
                || !envelope.TryGetProperty("error", out var error))
            {
                throw new AnkiConnectException("AnkiConnect returned an unexpected response envelope.");
            }

            if (error.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                throw new AnkiConnectException(error.GetString() ?? "AnkiConnect reported an unknown error.");
            }

            if (typeof(T) == typeof(JsonElement))
            {
                return (T)(object)result.Clone();
            }

            return result.Deserialize<T>(JsonOptions)
                ?? throw new AnkiConnectException("AnkiConnect returned an empty result.");
        }
        catch (AnkiConnectException)
        {
            throw;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AnkiConnectException(
                $"AnkiConnect at {Endpoint} did not respond before the request timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AnkiConnectException(
                $"Could not reach AnkiConnect at {Endpoint}. Start Anki Desktop and verify that the AnkiConnect add-on is running.",
                exception);
        }
        catch (JsonException exception)
        {
            throw new AnkiConnectException("AnkiConnect returned an invalid JSON response.", exception);
        }
    }

}
