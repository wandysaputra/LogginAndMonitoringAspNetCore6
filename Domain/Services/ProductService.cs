using Domain.Models;
using Domain.Services.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;
using Repository.Interfaces;

namespace Domain.Services;

public class ProductService : IProductService
{
    private readonly ILogger<ProductService> _logger;
    private readonly IProductRepository _productRepository;

    public ProductService(ILogger<ProductService> logger, IProductRepository productRepository )
    {
        _logger = logger;
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<Product>> GetProductsForCategoryAsync(string category)
    {
        _logger.LogInformation("Getting products in service for {category}", category);

        var products = await _productRepository.GetProductsAsync(category);
        
        return products.Adapt<IEnumerable<Product>>();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        _logger.LogInformation("Getting single product async in service for {id}", id);

        var product =  await _productRepository.GetProductByIdAsync(id);
        
        return product?.Adapt<Product>();
    }

    public IEnumerable<Product> GetProductsForCategory(string category)
    {
        return _productRepository.GetProducts(category).Adapt<IEnumerable<Product>>();
    }

    public Product? GetProductById(int id)
    {
        _logger.LogInformation("Getting single product in service for {id}", id);

        return _productRepository.GetProductById(id)?.Adapt<Product>();
    }
}