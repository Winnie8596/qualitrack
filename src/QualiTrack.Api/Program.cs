using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpLogging;
using QualiTrack.Api.Authentication;
using QualiTrack.Api.Http;
using QualiTrack.Api.Ncrs;
using QualiTrack.Api.Users;
using QualiTrack.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Not in appsettings: it holds a password. Set ConnectionStrings__QualiTrack in the environment or user secrets.
var connectionString = builder.Configuration.GetConnectionString("QualiTrack")
    ?? throw new InvalidOperationException("Connection string 'QualiTrack' is not configured.");

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ValidationFieldNames.UseJsonNames);
builder.Services.AddValidation();

// Development otherwise throws on a body that cannot be read, which the exception handler turns into a 500;
// this answers 400 in every environment, as production already does
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

// Enums travel as their names ("Critical", "Open"), so the web app does not depend on the enums' numbering,
// and a number in their place is refused rather than read as whichever value has it
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddHealthChecks();

// One structured log entry per request, written when it ends, with its duration (the category is enabled in
// appsettings). No headers, query string or bodies: they can hold cookies and personal data.
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});
builder.Services.AddPersistence(connectionString);
builder.Services.AddCookieAuthentication();
builder.Services.AddLoginRateLimit(builder.Configuration);
builder.Services.AddAntiforgeryProtection();
builder.Services.AddNcrUseCases();
builder.Services.AddUserUseCases();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Convenient locally; in production, migrations are a deliberate deployment step, not a side effect of startup
    await app.Services.MigrateDatabaseAsync();
    await app.Services.SeedDemoUsersAsync();
}

// First, so every response gets them, whatever later in the pipeline answers it
app.UseSecurityHeaders();
app.UseCorrelationId();
app.UseHttpLogging();

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
app.MapNcrEndpoints();
app.MapUserEndpoints();

await app.RunAsync();
