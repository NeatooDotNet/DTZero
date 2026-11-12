using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using DTZero;
using Person.DomainModel;
using Person.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

// Incorporate DTZero
builder.Services.AddScoped<IFactoryEventRelay, PersonEventHandler>();
builder.Services.AddDTZero(DTZeroFactory.Remote, typeof(IPersonModel).Assembly, typeof(PersonEventHandler).Assembly);
builder.Services.AddKeyedScoped(DTZero.DTZeroServices.HttpClientKey, (sp, key) => {
		return new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);
builder.Services.AddScoped<IUser, User>();

await builder.Build().RunAsync();
