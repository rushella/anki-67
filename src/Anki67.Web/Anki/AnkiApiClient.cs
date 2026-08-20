using System.Net.Http.Json;
using System.Text.Json;

namespace Anki67.Web.Anki;

public sealed class AnkiApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<AnkiConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        GetAsync<AnkiConnectionStatus>("api/anki/status", cancellationToken);

    public Task<string[]> GetDecksAsync(CancellationToken cancellationToken = default) =>
        GetAsync<string[]>("api/anki/decks", cancellationToken);

    public Task<AnkiNoteSearchResult> SearchNotesAsync(
        string query,
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        GetAsync<AnkiNoteSearchResult>(
            $"api/anki/notes?query={Uri.EscapeDataString(query)}&limit={limit}",
            cancellationToken);

    public async Task<AnkiNote> UpdateNoteAsync(
        long noteId,
        UpdateAnkiNote update,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.PutAsJsonAsync($"api/anki/notes/{noteId}", update, JsonOptions, cancellationToken),
            cancellationToken);

        return await ReadSuccessAsync<AnkiNote>(response, cancellationToken);
    }

    public async Task<AnkiSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.PostAsync("api/anki/sync", null, cancellationToken),
            cancellationToken);

        return await ReadSuccessAsync<AnkiSyncResult>(response, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync(requestUri, cancellationToken),
            cancellationToken);

        return await ReadSuccessAsync<T>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            return await send();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AnkiApiException("The Anki67 API request timed out.");
        }
        catch (HttpRequestException exception)
        {
            var endpoint = httpClient.BaseAddress?.ToString().TrimEnd('/');
            throw new AnkiApiException(
                $"Could not reach the Anki67 API at {endpoint}. Start Anki67.Api and try again.",
                exception);
        }
    }

    private static async Task<T> ReadSuccessAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new AnkiApiException(await ReadErrorAsync(response, cancellationToken));
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                ?? throw new AnkiApiException("The Anki67 API returned an empty response.");
        }
        catch (JsonException exception)
        {
            throw new AnkiApiException("The Anki67 API returned invalid JSON.", exception);
        }
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            foreach (var propertyName in new[] { "detail", "error", "title" })
            {
                if (root.TryGetProperty(propertyName, out var value)
                    && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    return value.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
            // Fall back to the HTTP status below.
        }

        return $"The Anki67 API returned {(int)response.StatusCode} {response.ReasonPhrase}.";
    }
}
