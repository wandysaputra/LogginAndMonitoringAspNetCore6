using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebApp;

public class UserScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserScopeMiddleware> _logger;

    public UserScopeMiddleware(RequestDelegate next, ILogger<UserScopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        var state = new Dictionary<string, object>();
        state.TryAdd("CorrelationId", correlationId);
        state.TryAdd("RequestHost", context.Request.Host.Value);
        state.TryAdd("RequestScheme", context.Request.Scheme);
        state.TryAdd("RemoteIpAddress", context.Connection.RemoteIpAddress ?? new IPAddress(0));

        if (context.User.Identity is { IsAuthenticated: true })
        {
            var user = context.User;
            var pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";
            var maskedUsername = Regex.Replace(user.Identity.Name??"", pattern, m => new string('*', m.Length));

            var subjectId = user.Claims.First(c=> c.Type == "sub")?.Value;

            state.TryAdd("User", maskedUsername);
            state.TryAdd("SubjectId", subjectId);
        }
        
        using (_logger.BeginScope(state))
        {
            await _next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        string str = string.Empty;
        StringValues source;
        if (context.Request.Headers.TryGetValue(CorrelationIdConstants.CorrelationIdHeaderKey, out source))
            str = source.FirstOrDefault<string>();
        else if (context.Response.Headers.TryGetValue(CorrelationIdConstants.CorrelationIdHeaderKey, out source))
            str = source.FirstOrDefault<string>();
        string correlationId = string.IsNullOrEmpty(str) ? Guid.NewGuid().ToString() : str;
        return correlationId;
    }
}