using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using OrdersPipeline.Api.Data;
using OrdersPipeline.Api.Common.Models;

namespace OrdersPipeline.Api.Features.Products.List;

internal static class ListProductsEndpoint
{
    private sealed record ProductResponse(
        Guid Id,
        string Name,
        string Category,
        decimal Price,
        int Stock,
        string Supplier);

    public static IEndpointRouteBuilder MapListProductsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/products", HandleAsync)
            .WithName("ListProducts");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        OrdersDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 10)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination parameters",
                detail: "Page must be at least 1 and page_size must be between 1 and 100.");
        }

        var totalCount = await dbContext.Products.CountAsync(cancellationToken);
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Category,
                product.Price,
                product.Stock,
                product.Supplier))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResponse<ProductResponse>(
            products,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)));
    }
}
