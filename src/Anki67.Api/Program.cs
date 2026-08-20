using Anki67.Api.Anki;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<AnkiConnectOptions>()
    .Bind(builder.Configuration.GetSection(AnkiConnectOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme is "http" or "https",
        "AnkiConnect:Endpoint must be an absolute HTTP or HTTPS URL.")
    .Validate(
        options => options.TimeoutSeconds is >= 1 and <= 120,
        "AnkiConnect:TimeoutSeconds must be between 1 and 120.")
    .ValidateOnStart();

builder.Services.AddHttpClient<AnkiConnectClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<AnkiConnectOptions>>().Value;
    client.BaseAddress = new Uri($"{options.Endpoint.TrimEnd('/')}/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var webOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5184", "https://localhost:7184"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Anki67Web", policy =>
        policy
            .WithOrigins(webOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Anki67Web");

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapAnkiEndpoints();

app.Run();
