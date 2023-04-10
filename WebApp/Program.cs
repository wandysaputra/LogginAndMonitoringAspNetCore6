using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using WebApp;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
// builder.Logging.AddDebug();
builder.Services.AddApplicationInsightsTelemetry();

/*
builder.Services.AddHttpLogging(logging =>
{
    // https://bit.ly/aspnetcore6-httplogging
    logging.LoggingFields = HttpLoggingFields.All;
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
builder.Services.AddW3CLogging(option =>
{
    // https://bit.ly/aspnetcore6-w3clogger
    option.LoggingFields = W3CLoggingFields.All;
    option.FileSizeLimit = 5 * 1024 * 1024;
    option.RetainedFileCountLimit = 2;
    option.FileName = "Logging-W3C-UI";
    option.LogDirectory = Path.Combine(path, "logs");
    option.FlushInterval = TimeSpan.FromSeconds(2);
});
*/

// create an HttpClient used for accessing the API
builder.Services.AddHttpClient("APIClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7018/api/");
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
}).AddUserAccessTokenHandler(); // to handle Bearer required by API authentication

// create an HttpClient used for accessing the API
builder.Services.AddHttpClient("IDPClient", client =>
{
    client.BaseAddress = new Uri("https://demo.duendesoftware.com");
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAccessTokenManagement(); // https://identitymodel.readthedocs.io/en/latest/aspnetcore/web.html
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies", options =>
    {
        options.Events.OnSigningOut = async e =>
        {
            // revoke refresh token on sign-out
            await e.HttpContext.RevokeUserRefreshTokenAsync();
        };
        options.AccessDeniedPath = "/Authentication/AccessDenied"; // handles redirection when access is denied
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.ClientId = "interactive.confidential";
        options.ClientSecret = "secret";
        
        // code flow + PKCE (PKCE is turned on by default)
        options.ResponseType = "code";
        
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("api");
        options.Scope.Add("offline_access");
        
        // keeps id_token smaller
        options.GetClaimsFromUserInfoEndpoint = true;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email"
        };
        options.SaveTokens = true;
    });
builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();
/*
app.UseHttpLogging();
app.UseW3CLogging();
*/
// Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
// }

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<UserScopeMiddleware>();

app.UseAuthorization();

app.MapRazorPages()
    .RequireAuthorization();

app.Run();
