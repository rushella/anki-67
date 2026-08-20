# Anki67

Anki67 is a .NET 10 Blazor WebAssembly app with an ASP.NET Core API for searching and editing notes in Anki Desktop.

## How the Anki connection works

AnkiWeb doesn't expose a supported card-editing REST API. Anki67 uses the AnkiConnect add-on's local API instead:

```text
Blazor WebAssembly → Anki67.Api → AnkiConnect → Anki Desktop → AnkiWeb sync
```

The API proxy keeps the optional AnkiConnect API key out of the browser and provides controlled CORS, timeouts, and friendly connection errors.

## Set up AnkiConnect

1. Install and open Anki Desktop.
2. In Anki, select **Tools → Add-ons → Get Add-ons**.
3. Enter add-on code `2055492159`.
4. Restart Anki and leave it running while using Anki67.
5. Verify AnkiConnect by opening <http://127.0.0.1:8765> on the same machine as Anki. It should display `Anki-Connect`.

Anki67.Api connects to `http://127.0.0.1:8765` by default. The endpoint is configured in `src/Anki67.Api/appsettings.json`.

## Run the application

Use two terminals from the repository root:

```bash
dotnet run --project src/Anki67.Api
```

```bash
dotnet run --project src/Anki67.Web
```

Open <http://localhost:5184/anki>.

The Anki page can:

- check the AnkiConnect connection and active profile;
- filter by deck and use standard Anki search syntax;
- edit every field and the tags on a note;
- save changes directly to Anki Desktop;
- trigger an explicit sync from Anki Desktop to AnkiWeb.

## Optional API key

AnkiConnect authentication is disabled by default. To enable it, set `apiKey` in **Tools → Add-ons → AnkiConnect → Config**, then store the same value in .NET user secrets:

```bash
dotnet user-secrets set "AnkiConnect:ApiKey" "your-api-key" --project src/Anki67.Api
```

Never commit the API key to `appsettings.json`.

## WSL with Anki running on Windows

If Anki67 runs in WSL and Anki Desktop runs on Windows, first try the default endpoint. Recent WSL networking configurations may make the Windows loopback service reachable directly.

If the connection check still fails:

1. Determine an address where the Windows host is reachable from WSL.
2. Configure AnkiConnect's `webBindAddress` to a suitable Windows host address. Binding to `0.0.0.0` exposes it on every interface and should only be used with a firewall and an API key.
3. Configure the endpoint without committing it:

```bash
dotnet user-secrets set "AnkiConnect:Endpoint" "http://WINDOWS_HOST_IP:8765" --project src/Anki67.Api
```

4. Restart Anki Desktop and Anki67.Api.

## Local API

The API is intended to remain local. Its endpoints are:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/anki/status` | Test AnkiConnect and report profile/version |
| `GET` | `/api/anki/decks` | List decks |
| `GET` | `/api/anki/notes?query=...&limit=50` | Search and load notes |
| `GET` | `/api/anki/notes/{noteId}` | Load one note |
| `PUT` | `/api/anki/notes/{noteId}` | Replace selected fields and/or tags |
| `POST` | `/api/anki/sync` | Sync the local collection with AnkiWeb |

Don't expose Anki67.Api or AnkiConnect directly to an untrusted network without adding authentication and TLS.
