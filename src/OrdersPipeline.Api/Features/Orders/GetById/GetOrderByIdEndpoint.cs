using Microsoft.EntityFrameworkCore;
using OrdersPipeline.Api.Data;
using OrdersPipeline.Api.Features.Orders.Shared;

namespace OrdersPipeline.Api.Features.Orders.GetById;

internal static class GetOrderByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetOrderByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/orders/{id:guid}", HandleAsync)
            .WithName("GetOrderById");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        OrdersDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(currentOrder => currentOrder.Items)
            .SingleOrDefaultAsync(currentOrder => currentOrder.Id == id, cancellationToken);

        if (order is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Order not found",
                detail: $"Order '{id}' was not found.");
        }

        return Results.Ok(new OrderResponse(
            order.Id,
            order.CreatedAt,
            order.Status,
            [.. order.Items.Select(item => new OrderItemResponse(
                item.ProductId,
                item.Quantity,
                item.UnitPrice))]));
    }
}
