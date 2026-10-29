using QualiTrack.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Not in appsettings: it holds a password. Set ConnectionStrings__QualiTrack in the environment or user secrets.
var connectionString = builder.Configuration.GetConnectionString("QualiTrack")
    ?? throw new InvalidOperationException("Connection string 'QualiTrack' is not configured.");

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddPersistence(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Convenient locally; in production, migrations are a deliberate deployment step, not a side effect of startup
    await app.Services.MigrateDatabaseAsync();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

await app.RunAsync();
