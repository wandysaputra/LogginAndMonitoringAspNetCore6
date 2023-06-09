﻿using Domain.Models;
using Domain.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;
    
    [LoggerMessage(Events.GettingProducts, LogLevel.Information, "SourceGenerated - Getting products in API.")]
    partial void LogGettingProduct();
    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<Product>> Get(string category = "all")
    {
        using (_logger.BeginScope("ScopeCategory: {category}", category))
        {
            LogGettingProduct();
            // _logger.LogInformation(Events.GettingProducts, "Getting products from API.");
            return await _productService.GetProductsForCategoryAsync(category);
        }
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