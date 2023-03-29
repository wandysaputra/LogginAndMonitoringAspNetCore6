using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository;

public class ProductRepository : IProductRepository
{
    private readonly LocalContext _ctx;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(LocalContext ctx, ILogger<ProductRepository> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task<List<Product>> GetProductsAsync(string category)
    {
        _logger.LogInformation("Getting products async in repository for {category}", category);

        return await _ctx.Products.Where(p => p.Category == category || category == "all").ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        _logger.LogInformation("Getting single product async in repository for {id}", id);

        return await _ctx.Products.FindAsync(id);
    }

    public List<Product> GetProducts(string category)
    {
        _logger.LogInformation("Getting products in repository for {category}", category);

        return _ctx.Products.Where(p => p.Category == category || category == "all").ToList();
    }

    public Product? GetProductById(int id)
    {
        var timer = new Stopwatch();
        timer.Start();
        var productById = _ctx.Products.Find(id);
        timer.Stop();

        _logger.LogDebug("Querying products for {id} finished in {milliseconds} milliseconds", id, timer.ElapsedMilliseconds);
        return productById;
    }
}