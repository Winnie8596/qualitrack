using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QualiTrack.Api.IntegrationTests.Authentication;

[Collection(ApiCollection.Name)]
public sealed class AuthenticationCookieTests(QualiTrackApiFactory api)
{
    [Fact]
    public void The_session_cookie_is_hidden_from_scripts_sent_only_over_https_and_never_cross_site()
    {
        var cookie = CookieOptions().Cookie;

        Assert.True(cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Strict, cookie.SameSite);
    }

    [Fact]
    public async Task A_signed_out_caller_gets_401_instead_of_a_redirect_to_a_login_page()
    {
        var context = RedirectContext();

        await CookieOptions().Events.RedirectToLogin(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task A_caller_without_the_right_role_gets_403_instead_of_a_redirect_to_an_access_denied_page()
    {
        var context = RedirectContext();

        await CookieOptions().Events.RedirectToAccessDenied(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    private CookieAuthenticationOptions CookieOptions() =>
        api.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

    private RedirectContext<CookieAuthenticationOptions> RedirectContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/ncrs";
        var scheme = new AuthenticationScheme(
            IdentityConstants.ApplicationScheme, displayName: null, typeof(CookieAuthenticationHandler));
        return new(httpContext, scheme, CookieOptions(), new AuthenticationProperties(), "/Account/Login");
    }
}
