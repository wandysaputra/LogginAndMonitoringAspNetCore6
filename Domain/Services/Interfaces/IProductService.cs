using Domain.Models;

namespace Domain.Services.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetProductsForCategoryAsync(string category);
    Task<Product?> GetProductByIdAsync(int id);
    IEnumerable<Models.Product> GetProductsForCategory(string category);
    Product? GetProductById(int id);
}