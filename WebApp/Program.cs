using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
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
app.UseAuthorization();

app.MapRazorPages()
    .RequireAuthorization();

app.Run();
