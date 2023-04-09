using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Models;

namespace WebApp.Pages;

public partial class ListingModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ListingModel> _logger;

    public ListingModel(HttpClient apiClient, IHttpClientFactory httpClientFactory, ILogger<ListingModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public List<Product> Products { get; set; }
    public string CategoryName { get; set; } = "";

    [LoggerMessage(0, LogLevel.Warning, "SourceGenerated - API failure: {fullPath} Response: {statusCode}, Trace: {traceId}")]
    partial void LogApiFailure(string fullPath, int statusCode, string traceId);

    public async Task OnGetAsync()
    {
        var cat = Request.Query["cat"].ToString();
        if (string.IsNullOrEmpty(cat))
        {
            throw new Exception("failed");
        }
        var apiClient = _httpClientFactory.CreateClient("APIClient");
        var response = await apiClient.GetAsync($"Product?category={cat}");
        if (!response.IsSuccessStatusCode)
        {
            var fullPath = $"{apiClient.BaseAddress}Product?category={cat}";

            var details = await response.Content.ReadFromJsonAsync<ProblemDetails>() ?? new ProblemDetails();
            var traceId = details.Extensions["traceId"]?.ToString();

            LogApiFailure(fullPath, (int)response.StatusCode, traceId ?? string.Empty);
            // _logger.LogWarning("API failure: {fullPath} Response: {response}, Trace: {trace}, User: {user}", fullPath, (int)response.StatusCode, traceId, userName);

            throw new Exception("API call failed!");
        }

        Products = await response.Content.ReadFromJsonAsync<List<Product>>() ?? new List<Product>();
        if (Products.Any())
        {
            CategoryName = Products.First().Category.First().ToString().ToUpper() +
                           Products.First().Category[1..];
        }
    }
}