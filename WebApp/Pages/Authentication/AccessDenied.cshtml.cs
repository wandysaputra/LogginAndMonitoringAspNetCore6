using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace WebApp.Pages.Authentication;

public partial class AccessDeniedModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AccessDeniedModel> _logger;

    public AccessDeniedModel(IHttpClientFactory httpClientFactory, ILogger<AccessDeniedModel> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
    }

    [Authorize]
    public async Task OnGet()
    {
        var clientIDP = _httpClientFactory.CreateClient("IDPClient");

        var discoveryDocumentResponse = await clientIDP.GetDiscoveryDocumentAsync();
        if (discoveryDocumentResponse?.IsError ?? true)
        {
            throw new Exception(discoveryDocumentResponse?.Error);
        }

        var accessTokenRevocationResponse = await clientIDP.RevokeTokenAsync(new()
        {
            Address = discoveryDocumentResponse.RevocationEndpoint
            , ClientId = "interactive.confidential"
            , ClientSecret = "secret"
            , Token = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken)
        });

        if (accessTokenRevocationResponse?.IsError ?? true)
        {
            throw new Exception(accessTokenRevocationResponse?.Error);
        }

        var refreshTokenRevocationResponse = await clientIDP.RevokeTokenAsync(new()
        {
            Address = discoveryDocumentResponse.RevocationEndpoint
            , ClientId = "interactive.confidential"
            , ClientSecret = "secret"
            , Token = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken)
        });

        if (refreshTokenRevocationResponse?.IsError ?? true)
        {
            throw new Exception(refreshTokenRevocationResponse?.Error);
        }

        // Clears the local cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirects to the IDP linked to scheme `OpenIdConnectDefaults.AuthenticationScheme` so it can clear its own session/cookie
        await HttpContext.SignOutAsync("oidc");
    }
}