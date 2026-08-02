namespace OrdersPipeline.Api.Models;

internal sealed class StockEntry
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public required string SourceEventId { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<OrderStockEntry> Orders { get; set; } = [];
}
