using Repository.Entities;

namespace Repository.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetProductsAsync(string category);
    Task<Product?> GetProductByIdAsync(int id);
    List<Product> GetProducts(string category);
    Product? GetProductById(int id);
}