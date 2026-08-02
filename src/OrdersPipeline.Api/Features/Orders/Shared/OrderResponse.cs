using OrdersPipeline.Api.Models;

namespace OrdersPipeline.Api.Features.Orders.Shared;

internal sealed record OrderResponse(
    Guid Id,
    DateTime CreatedAt,
    OrderStatus Status,
    IReadOnlyCollection<OrderItemResponse> Items);
