using Microsoft.EntityFrameworkCore;
using OrdersPipeline.Api.Data;
using OrdersPipeline.Api.Features.Orders.Shared;
using OrdersPipeline.Api.Models;

namespace OrdersPipeline.Api.Features.Orders.Create;

internal static class CreateOrderEndpoint
{
    private sealed record CreateOrderRequest(IReadOnlyCollection<CreateOrderItemRequest> Items);

    private sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);

    public static IEndpointRouteBuilder MapCreateOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/orders", HandleAsync)
            .WithName("CreateOrder");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        CreateOrderRequest request,
        OrdersDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid order",
                detail: "An order must contain at least one item.");
        }

        if (request.Items.Any(item => item.Quantity < 1))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid order item",
                detail: "Item quantity must be at least 1.");
        }

        if (request.Items.Select(item => item.ProductId).Distinct().Count() != request.Items.Count)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid order",
                detail: "A product can only appear once in an order.");
        }

        var productIds = request.Items.Select(item => item.ProductId).ToArray();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var missingProductIds = productIds.Where(productId => !products.ContainsKey(productId)).ToArray();
        if (missingProductIds.Length > 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Product not found",
                detail: $"The following product identifiers do not exist: {string.Join(", ", missingProductIds)}.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = [.. request.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Product = products[item.ProductId],
                Quantity = item.Quantity,
                UnitPrice = products[item.ProductId].Price
            })]
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/orders/{order.Id}", new OrderResponse(
            order.Id,
            order.CreatedAt,
            order.Status,
            [.. order.Items.Select(item => new OrderItemResponse(
                item.ProductId,
                item.Quantity,
                item.UnitPrice))]));
    }
}
