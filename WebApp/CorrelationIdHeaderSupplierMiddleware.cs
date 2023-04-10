using Microsoft.Extensions.Primitives;

namespace WebApp;

public class CorrelationIdHeaderSupplierMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerKey;

    public CorrelationIdHeaderSupplierMiddleware(RequestDelegate next, string headerKey = CorrelationIdConstants.CorrelationIdHeaderKey)
    {
        _next = next;
        _headerKey = headerKey;
    }

    public Task Invoke(HttpContext httpContext)
    {
        var correlationId = GetCorrelationId(httpContext);

        if (!httpContext.Items.ContainsKey(CorrelationIdConstants.CorrelationIdItemName))
        {
            httpContext.Items.Add(CorrelationIdConstants.CorrelationIdItemName, correlationId);
        }

        if (!httpContext.Response.Headers.ContainsKey(_headerKey))
        {
            httpContext.Response.Headers.Add(_headerKey, correlationId);
        }

        return _next(httpContext);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        string str = string.Empty;
        StringValues source;
        if (context.Request.Headers.TryGetValue(CorrelationIdConstants.CorrelationIdHeaderKey, out source))
            str = source.FirstOrDefault<string>() ?? string.Empty;
        else if (context.Response.Headers.TryGetValue(CorrelationIdConstants.CorrelationIdHeaderKey, out source))
            str = source.FirstOrDefault<string>() ?? string.Empty;
        string correlationId = string.IsNullOrEmpty(str) ? Guid.NewGuid().ToString() : str;
        return correlationId;
    }
}

public static class CorrelationIdHeaderSupplierMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationIdHeaderSupplier(this IApplicationBuilder builder, string headerKey = CorrelationIdConstants.CorrelationIdHeaderKey)
    {
        return builder.UseMiddleware<CorrelationIdHeaderSupplierMiddleware>(headerKey);
    }
}

public class CorrelationIdConstants
{
    public const string CorrelationIdItemName = "CorrelationIdEnricher+CorrelationId";
    public const string CorrelationIdHeaderKey = "x-correlation-id";
}