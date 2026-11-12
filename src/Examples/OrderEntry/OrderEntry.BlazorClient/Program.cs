using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using DTZero;
using OrderEntry.BlazorClient;
using OrderEntry.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

// Register DTZero with remote factories from Domain
builder.Services.AddDTZero(DTZeroFactory.Remote, typeof(IOrder).Assembly);
builder.Services.AddKeyedScoped(DTZeroServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

await builder.Build().RunAsync();
