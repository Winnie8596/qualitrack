using System.Threading.RateLimiting;

namespace QualiTrack.Api.Authentication;

/// <summary>
/// Caps login attempts per client address, to slow password guessing across many accounts (PRD §7).
/// Lockout already slows guessing at any one account; this covers trying one password on many.
/// </summary>
public static class LoginRateLimit
{
    public const string Policy = "login";

    private const string Section = "LoginRateLimit";

    public static IServiceCollection AddLoginRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue($"{Section}:PermitLimit", defaultValue: 5);
        var window = TimeSpan.FromSeconds(configuration.GetValue($"{Section}:WindowSeconds", defaultValue: 60));

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));
        });
    }
}
