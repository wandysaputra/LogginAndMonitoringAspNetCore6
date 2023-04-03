using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository;

public class ProductRepository : IProductRepository
{
    private readonly LocalContext _ctx;
    private readonly ILogger<ProductRepository> _logger;
    private readonly ILogger _factoryLogger;

    public ProductRepository(LocalContext ctx, ILogger<ProductRepository> logger, ILoggerFactory loggerFactory)
    {
        _ctx = ctx;
        _logger = logger;
        _factoryLogger = loggerFactory.CreateLogger("DataAccessLayer");
    }
    public async Task<List<Product>> GetProductsAsync(string category)
    {
        _logger.LogInformation("Getting products async in repository for {category}", category);
        if (category == "clothing")
        {
            var ex = new ApplicationException("Database error occurred!!");
            ex.Data.Add("Category", category);
            throw ex;
        }
        if (category == "equip")
        {
            throw new SqliteException("Simulated fatal database error occurred!", 551);
        }

        try
        {
            return await _ctx.Products.Where(p => p.Category == category || category == "all").ToListAsync();
        }
        catch (Exception ex)
        {
            var newEx = new ApplicationException("Something bad happened in database", ex);
            newEx.Data.Add("Category", category);
            throw newEx;
        }
        
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
        _factoryLogger.LogInformation("[DAL] Querying products for {id} finished in {ticks} ticks", id, timer.ElapsedTicks);

        return productById;
    }
}