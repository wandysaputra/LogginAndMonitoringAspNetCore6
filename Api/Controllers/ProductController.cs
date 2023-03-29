using Domain.Models;
using Domain.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<Product>> Get(string category = "all")
    {
        _logger.LogInformation("Getting products from API for {category}", category);
        return await _productService.GetProductsForCategoryAsync(category);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(int id)
    {
        _logger.LogDebug("Getting single product in API for {id}", id);

        // var product = await _productService.GetProductByIdAsync(id);
        var product = _productService.GetProductById(id);


        if (product != null)
        {
            return Task.FromResult<IActionResult>(Ok(product));
        }

        _logger.LogWarning("No product found for ID: {id}", id);

        return Task.FromResult<IActionResult>(NotFound());
    }
}