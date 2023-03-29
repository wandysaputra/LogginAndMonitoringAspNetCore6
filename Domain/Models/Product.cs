﻿namespace Domain.Models;

public class Product
{
    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public double Price { get; }
    public string Category { get; }

    public Product(int id, string name, string description, double price, string category)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Category = category;
    }
}