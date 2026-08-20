namespace Anki67.Api.Anki;

public static class AnkiEndpoints
{
    public static RouteGroupBuilder MapAnkiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/anki");

        group.MapGet("/status", GetStatusAsync);
        group.MapGet("/decks", GetDecksAsync);
        group.MapGet("/notes", SearchNotesAsync);
        group.MapGet("/notes/{noteId:long}", GetNoteAsync);
        group.MapPut("/notes/{noteId:long}", UpdateNoteAsync);
        group.MapPost("/sync", SyncAsync);

        return group;
    }

    private static async Task<IResult> GetStatusAsync(
        AnkiConnectClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var version = await client.GetVersionAsync(cancellationToken);
            var profile = await client.GetActiveProfileAsync(cancellationToken);
            var decks = await client.GetDeckNamesAsync(cancellationToken);

            return Results.Ok(new AnkiConnectionStatusResponse(
                true,
                version,
                profile,
                decks.Count,
                client.Endpoint,
                $"Connected to AnkiConnect API v{version}."));
        }
        catch (AnkiConnectException exception)
        {
            return Results.Ok(new AnkiConnectionStatusResponse(
                false,
                null,
                null,
                0,
                client.Endpoint,
                exception.Message));
        }
    }

    private static async Task<IResult> GetDecksAsync(
        AnkiConnectClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await client.GetDeckNamesAsync(cancellationToken));
        }
        catch (AnkiConnectException exception)
        {
            return AnkiProblem(exception);
        }
    }

    private static async Task<IResult> SearchNotesAsync(
        string? query,
        int? limit,
        AnkiConnectClient client,
        CancellationToken cancellationToken)
    {
        var requestedLimit = limit ?? 50;
        if (requestedLimit is < 1 or > 100)
        {
            return Results.BadRequest(new { error = "Limit must be between 1 and 100." });
        }

        if (query?.Length > 1_000)
        {
            return Results.BadRequest(new { error = "The Anki search query is too long." });
        }

        try
        {
            return Results.Ok(await client.SearchNotesAsync(
                query?.Trim() ?? string.Empty,
                requestedLimit,
                cancellationToken));
        }
        catch (AnkiConnectException exception)
        {
            return AnkiProblem(exception);
        }
    }

    private static async Task<IResult> GetNoteAsync(
        long noteId,
        AnkiConnectClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await client.GetNoteAsync(noteId, cancellationToken);
            return note is null ? Results.NotFound() : Results.Ok(note);
        }
        catch (AnkiConnectException exception)
        {
            return AnkiProblem(exception);
        }
    }

    private static async Task<IResult> UpdateNoteAsync(
        long noteId,
        UpdateAnkiNoteRequest request,
        AnkiConnectClient client,
        CancellationToken cancellationToken)
    {
        if (request.Fields is null && request.Tags is null)
        {
            return Results.BadRequest(new { error = "Provide fields, tags, or both." });
        }

        if (request.Fields?.Keys.Any(string.IsNullOrWhiteSpace) is true)
        {
            return Results.BadRequest(new { error = "Anki field names cannot be empty." });
        }

        var tags = request.Tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        try
        {
            var note = await client.UpdateNoteAsync(
                noteId,
                request.Fields,
                tags,
                cancellationToken);

            return note is null ? Results.NotFound() : Results.Ok(note);
        }
        catch (AnkiConnectException exception)
        {
            return AnkiProblem(exception);
        }
    }

    private static async Task<IResult> SyncAsync(
        AnkiConnectClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.SyncAsync(cancellationToken);
            return Results.Ok(new AnkiSyncResponse(
                DateTimeOffset.UtcNow,
                "Anki sync completed."));
        }
        catch (AnkiConnectException exception)
        {
            return AnkiProblem(exception);
        }
    }

    private static IResult AnkiProblem(AnkiConnectException exception) =>
        Results.Problem(
            title: "AnkiConnect request failed",
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
}
