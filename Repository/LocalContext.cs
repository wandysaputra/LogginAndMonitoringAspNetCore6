using Microsoft.EntityFrameworkCore;
using Repository.Entities;

namespace Repository;

public class LocalContext : DbContext
{
    public DbSet<Product> Products { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source=product-logging.db");

    public void MigrateAndCreateData()
    {
        Database.Migrate();

        if (Products.Any())
        {
            return;
        }

        Products.Add(new Product
        {
            Name = "Trailblazer",
            Category = "boots",
            Price = 69.99,
            Description = "Great support in this high-top to take you to great heights and trails.",
            ImgUrl = "/images/img-brownboots.jpg"
        });
        Products.Add(new Product
        {
            Name = "Coastliner",
            Category = "boots",
            Price = 49.99,
            Description =
                "Easy in and out with this lightweight but rugged shoe with great ventilation to get your around shores, beaches, and boats.",
            ImgUrl = "/images/img-greyboots.jpg"
        });
        Products.Add(new Product
        {
            Name = "Woodsman",
            Category = "boots",
            Price = 64.99,
            Description =
                "All the insulation and support you need when wandering the rugged trails of the woods and backcountry.",
            ImgUrl = "/images/shutterstock_222721876.jpg"
        });
        Products.Add(new Product
        {
            Name = "Billy",
            Category = "boots",
            Price = 79.99,
            Description =
                "Get up and down rocky terrain like a billy-goat with these awesome high-top boots with outstanding support.",
            ImgUrl = "/images/shutterstock_475046062.jpg"
        });

        SaveChanges();
    }
}