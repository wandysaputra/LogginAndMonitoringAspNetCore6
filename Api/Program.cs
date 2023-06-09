using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Api;
// using Api;
using Domain.Services;
using Domain.Services.Interfaces;
using Hellang.Middleware.ProblemDetails;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Repository;
using Repository.Interfaces;
using Serilog;
using Serilog.Context;
using Serilog.Enrichers.Span;
using Serilog.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;
using CorrelationIdHeaderSupplierMiddlewareExtensions = WebApp.CorrelationIdHeaderSupplierMiddlewareExtensions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using UserScopeMiddleware = WebApp.UserScopeMiddleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
// builder.Logging.AddJsonConsole();
// builder.Logging.AddDebug();
// builder.Logging.AddSimpleConsole();
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
        // .Enrich.WithCorrelationId()
        .Enrich.WithCorrelationIdHeader()
        .Enrich.WithClientIp()
        .Enrich.WithClientAgent()
        .Enrich.FromLogContext() // Enrich log events with properties from Context.LogContext.
        .Enrich
        .WithExceptionDetails() // Enrich logger output with a destructured object containing exception's public properties.
        /* Field                        Description                                             Example
         * ActionId                     Identifier for the action / route / page                de9ab21b-279b-42c6-b93e-b0d377c49f8e
         * ActionName                   Name of the action / route / page                       Api.Controllers.ProductController.Get(Api)
         *                                                                                      /Listing
         * ConnectionID                 Can be shared across multiple navigations;              0HMFC25O20STO
         *                              can change within session.          
         * RequestID                    Combination of RequestID and a sequence                 0HMFC25O20STO:00000007
         * (HttpContext.TraceIdentifer) number for a request within a Connection            
         * TraceId                      Identifier for a logical transaction                    514b22c0573bf5b992354804a9993cac
         * SpanId                       Identifier for an individual activity within a          1b6142c5188baad6
         *                              trace (see TraceId)
         * ParentId                     Formatted like span id but the span id of               f7ac2e649b1eca3a
         *                              the activity that created the current one
         *                              (0’s if no parent)                                      0000000000000000
         */
        .Enrich.With<ActivityEnricher>()
        .WriteTo.Seq("http://localhost:5341");
});

/*
 * docker run --name jaegar -p 13133:13133 -p 16686:16686 -p 4317:55680 -d --restart=unless-stopped jaegertracing/opentelemetry-all-in-one
 */
builder.Services.AddOpenTelemetry()
    .WithTracing(providerBuilder =>
    {
        providerBuilder.SetResourceBuilder(
                ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName))
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
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

builder.Logging.AddFilter("", LogLevel.Debug);
//var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); //C:\Users\<user>\AppData\Local
//var tracePath = Path.Join(path, $"LoggingAndMonitoringAspNetCore_{DateTime.Now.ToString("yyyyMMdd-HHmm")}.txt");
//Trace.Listeners.Add(new TextWriterTraceListener(System.IO.File.CreateText(tracePath)));
//Trace.AutoFlush = true;

builder.Services.AddProblemDetails(config =>
{
    config.IncludeExceptionDetails = (context, exception) => false;
    config.OnBeforeWriteDetails = (context, details) =>
    {
        if (details.Status == 500)
        {
            details.Detail = "An error occurred in our API. Use the trace id when contacting us.";
        }
    };
    config.Rethrow<SqliteException>();
    config.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.Audience = "api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email"
        };
    });


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptions>();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddDbContext<LocalContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LocalContext>();

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

app.UseMiddleware<CriticalExceptionMiddleware>();
app.UseProblemDetails();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var context = serviceProvider.GetRequiredService<LocalContext>();
    context.MigrateAndCreateData();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("interactive.public.short");
        options.OAuthAppName("API");
        options.OAuthUsePkce();
    });
}

app.MapFallback(() => Results.Redirect("/swagger"));

// app.UseHttpsRedirection();  // commented so Seq in docker can access it to do health check (note: change http://localhost:5164/health to http://host.docker.internal:5164/health)

app.UseAuthentication();

app.UseMiddleware<UserScopeMiddleware>();

app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization();
app.MapHealthChecks("health").AllowAnonymous();

app.Run();
