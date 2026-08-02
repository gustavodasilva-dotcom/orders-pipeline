using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrdersPipeline.Api.Data;
using OrdersPipeline.Api.Features.Orders.Create;
using OrdersPipeline.Api.Features.Orders.GetById;
using OrdersPipeline.Api.Features.Products.List;
using OrdersPipeline.Api.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddRabbitMqDebeziumConsumer(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapListProductsEndpoint();
app.MapCreateOrderEndpoint();
app.MapGetOrderByIdEndpoint();

app.Run();
