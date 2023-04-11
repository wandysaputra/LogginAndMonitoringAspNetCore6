using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using NLog;
using NLog.Web;
using Serilog;
using Serilog.Exceptions;
using WebApp;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
// builder.Logging.AddJsonConsole();
// builder.Logging.AddDebug();
// builder.Services.AddApplicationInsightsTelemetry();

/* https://hub.docker.com/r/datalust/seq/
 * docker pull datalust/seq
 * docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
 */
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console() // Writes log events to System.Console
        .Enrich.WithCorrelationId()
        .Enrich.WithCorrelationIdHeader()
        .Enrich.WithClientIp()
        .Enrich.WithClientAgent()
        .Enrich.FromLogContext() // Enrich log events with properties from Context.LogContext.
        .Enrich
        .WithExceptionDetails() // Enrich logger output with a destructured object containing exception's public properties.
        .WriteTo.Seq("http://localhost:5341");
});

/* https://hub.docker.com/r/splunk/splunk/
 * docker pull splunk/splunk
 * docker run -d -p 8000:8000 -p 8088:8088 -e "SPLUNK_START_ARGS=--accept-license" -e "SPLUNK_PASSWORD=Password123" --name splunk splunk/splunk:latest
 * 1. Login to Splunk at localhost:8000
 * 2. go to Settings > Data Inputs > HTTP Event Collector > Global Settings
 * 3. Make sure it enables and un-check SSL for local dev also make sure HTTP port number 8088 mapped in docker command
 * 4. Create new token by click `New Token` button
 * 5. In `New Token` wizard, give the `Name` and set `main` index, click submit then copy the token provided and paste it into nlog.config
 */
// NLog.LogManager.Setup().LoadConfigurationFromFile();
// builder.Host.UseNLog();

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
builder.Services.AddHealthChecks()
    .AddIdentityServer(new Uri("https://demo.duendesoftware.com"), failureStatus: HealthStatus.Degraded);

var app = builder.Build();

//app.UseSerilogRequestLogging(options =>
//{
//    options.MessageTemplate =
//        "{RemoteIpAddress} {RequestScheme} {RequestHost} {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
//    options.EnrichDiagnosticContext = (
//        diagnosticContext,
//        httpContext) =>
//    {
//        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
//        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
//        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
//    };
//});
app.UseCorrelationIdHeaderSupplier();

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

// app.UseHttpsRedirection();  // commented so Seq in docker can access it to do health check (note: change http://localhost:5245/health to http://host.docker.internal:5245/health)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<UserScopeMiddleware>();

app.UseAuthorization();

app.MapRazorPages()
    .RequireAuthorization();
app.MapHealthChecks("health").AllowAnonymous();
app.Run();
