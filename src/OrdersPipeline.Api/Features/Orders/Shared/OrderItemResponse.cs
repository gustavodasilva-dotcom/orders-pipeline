namespace OrdersPipeline.Api.Features.Orders.Shared;

internal sealed record OrderItemResponse(Guid ProductId, int Quantity, decimal UnitPrice);
