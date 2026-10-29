using QualiTrack.Api.Authentication;
using QualiTrack.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Not in appsettings: it holds a password. Set ConnectionStrings__QualiTrack in the environment or user secrets.
var connectionString = builder.Configuration.GetConnectionString("QualiTrack")
    ?? throw new InvalidOperationException("Connection string 'QualiTrack' is not configured.");

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddPersistence(connectionString);
builder.Services.AddCookieAuthentication();
builder.Services.AddLoginRateLimit(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Convenient locally; in production, migrations are a deliberate deployment step, not a side effect of startup
    await app.Services.MigrateDatabaseAsync();
    await app.Services.SeedDemoUsersAsync();
}

// Every error is ProblemDetails (PRD §8): an unhandled exception becomes a 500 without its stack trace, and an
// error with no body of its own (401 from the cookie, 404 for an unknown route) gets one
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();

await app.RunAsync();
