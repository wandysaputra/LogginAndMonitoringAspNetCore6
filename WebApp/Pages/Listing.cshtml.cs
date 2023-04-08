using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Models;

namespace WebApp.Pages;

public class ListingModel : PageModel
{
    private readonly HttpClient _apiClient;
    private readonly ILogger<ListingModel> _logger;

    public ListingModel(HttpClient apiClient, ILogger<ListingModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _apiClient.BaseAddress = new Uri("https://localhost:7018/api/");
    }

    public List<Product> Products { get; set; }
    public string CategoryName { get; set; } = "";

    public async Task OnGetAsync()
    {
        var cat = Request.Query["cat"].ToString();
        if (string.IsNullOrEmpty(cat))
        {
            throw new Exception("failed");
        }

        var response = await _apiClient.GetAsync($"Product?category={cat}");
        if (!response.IsSuccessStatusCode)
        {
            var fullPath = $"{_apiClient.BaseAddress}Product?category={cat}";

            var details = await response.Content.ReadFromJsonAsync<ProblemDetails>() ?? new ProblemDetails();
            var traceId = details.Extensions["traceId"]?.ToString();
            var userName = User.Identity?.Name ?? string.Empty;

            _logger.LogWarning("API failure: {fullPath} Response: {response}, Trace: {trace}, User: {user}", fullPath,
                (int)response.StatusCode, traceId, userName);

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