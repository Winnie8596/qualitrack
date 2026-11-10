using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using QualiTrack.Application.Abstractions;
using QualiTrack.Infrastructure.Persistence;

namespace QualiTrack.Api.Authentication;

public static class AuthenticationSetup
{
    /// <summary>ASP.NET Core Identity on PostgreSQL, signed in with a session cookie (ADR 0006).</summary>
    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.AddAuthorizationBuilder().AddPolicies();
        services
            .AddIdentityCore<IdentityUser<Guid>>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<QualiTrackDbContext>()
            .AddSignInManager();
        services.AddHttpContextAccessor().AddScoped<ICurrentUser, HttpCurrentUser>();

        services.ConfigureApplicationCookie(options =>
        {
            // Scripts cannot read it (limits XSS), it never travels over plain HTTP, and the browser does not
            // attach it to requests started by other sites (first line of defence against CSRF)
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;

            // The React app owns the login page, so the server has no page to redirect to: it answers with the
            // status code and the app decides what to show
            options.Events.OnRedirectToLogin = context => RespondWith(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => RespondWith(context, StatusCodes.Status403Forbidden);
        });

        return services;
    }

    /// <summary>
    /// Keeps the keys that encrypt the session cookie and the antiforgery token in a folder that outlives the
    /// process, when DataProtection:KeysPath names one (a mounted volume in Docker). Without it the framework's
    /// default applies: fine on a developer's machine, but a new container would start with new keys and every
    /// signed-in user would be signed out.
    /// </summary>
    public static IServiceCollection AddSessionKeys(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtection = services.AddDataProtection();
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        return services;
    }

    private static Task RespondWith(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
