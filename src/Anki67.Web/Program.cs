using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Anki67.Web;
using Anki67.Web.Anki;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : configuredApiBaseUrl;

builder.Services.AddScoped(_ => new AnkiApiClient(new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute),
    Timeout = TimeSpan.FromSeconds(30)
}));

await builder.Build().RunAsync();
