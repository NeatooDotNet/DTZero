using Microsoft.EntityFrameworkCore;
using DTZero.AspNetCore;
using OrderEntry.Domain;
using OrderEntry.Ef;

var builder = WebApplication.CreateBuilder(args);

// DTZero - Register factories from Domain assembly
builder.Services.AddDTZeroAspNetCore(typeof(IOrder).Assembly);
builder.Services.AddScoped<IOrderEntryContext, OrderEntryContext>();
builder.Services.AddDbContext<OrderEntryContext>();

var app = builder.Build();

// Create database on startup (for demo purposes)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderEntryContext>();
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine($"Database created at: {db.DbPath}");
}

// Hosted Blazor WASM middleware
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseDTZero();

// Fallback: serve index.html for unmatched routes (SPA routing)
app.MapFallbackToFile("index.html");

await app.RunAsync();
