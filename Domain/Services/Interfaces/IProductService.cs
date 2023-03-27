using Domain.Models;

namespace Domain.Services.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetProductsForCategory(string category);
}