namespace OrdersPipeline.Api.Models;

internal sealed class Product
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Category { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public required string Supplier { get; set; }
}
