using Domain.Models;
using Domain.Services.Interfaces;

namespace Domain.Services;

public class ProductService : IProductService
{
    public Task<IEnumerable<Product>> GetProductsForCategory(string category)
    {
        return Task.FromResult(GetAllProducts().Where(a =>
            string.Equals("all", category, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(category, a.Category, StringComparison.InvariantCultureIgnoreCase)));
    }

    private static IEnumerable<Product> GetAllProducts()
    {
        return new List<Product>
        {
            new Product { Id = 1, Name = "Trailblazer", Category = "boots", Price = 69.99,
                Description = "Great support in this high-top to take you to great heights and trails." },
            new Product { Id = 2, Name = "Coastliner", Category = "boots", Price = 49.99,
                Description = "Easy in and out with this lightweight but rugged shoe with great ventilation to get your around shores, beaches, and boats." },
            new Product { Id = 3, Name = "Woodsman", Category = "boots", Price = 64.99,
                Description = "All the insulation and support you need when wandering the rugged trails of the woods and backcountry."},
            new Product { Id = 4, Name = "Billy", Category = "boots", Price = 79.99,
                Description = "Get up and down rocky terrain like a billy-goat with these awesome high-top boots with outstanding support." },
        };
    }
}