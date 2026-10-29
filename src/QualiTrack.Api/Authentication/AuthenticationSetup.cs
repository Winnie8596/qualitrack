using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using QualiTrack.Infrastructure.Persistence;

namespace QualiTrack.Api.Authentication;

public static class AuthenticationSetup
{
    /// <summary>ASP.NET Core Identity on PostgreSQL, signed in with a session cookie (ADR 0006).</summary>
    public static IServiceCollection AddCookieAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.AddAuthorization();
        services
            .AddIdentityCore<IdentityUser<Guid>>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<QualiTrackDbContext>()
            .AddSignInManager();

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

    private static Task RespondWith(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
