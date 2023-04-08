using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Api;
using Domain.Services;
using Domain.Services.Interfaces;
using Hellang.Middleware.ProblemDetails;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repository;
using Repository.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

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

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseMiddleware<UserScopeMiddleware>();

app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization();

app.Run();
