namespace OrdersPipeline.Api.Models;

internal sealed class OrderStockEntry
{
    public Guid OrderId { get; set; }

    public Guid StockEntryId { get; set; }

    public Order Order { get; set; } = null!;

    public required StockEntry StockEntry { get; set; }
}
