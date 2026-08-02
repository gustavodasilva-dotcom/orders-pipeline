using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrdersPipeline.Api.Data;
using OrdersPipeline.Api.Models;

namespace OrdersPipeline.Api.Infrastructure.Messaging;

internal sealed class OrdersDebeziumEventConsumer(
    IOptions<RabbitMqOptions> optionsAccessor,
    IServiceScopeFactory scopeFactory,
    ILogger<OrdersDebeziumEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = optionsAccessor.Value;
        var factory = new ConnectionFactory { Uri = new Uri(options.ConnectionString) };
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            options.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            options.QueueName,
            options.ExchangeName,
            options.RoutingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await HandleAsync(eventArgs.Body.ToArray(), stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "Invalid JSON in Debezium event. The message will not be requeued.");
                logger.LogDebug("Invalid Debezium payload: {Payload}", Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
            catch (KeyNotFoundException exception)
            {
                logger.LogError(exception, "Required field was missing from Debezium event. The message will not be requeued.");
                logger.LogDebug("Malformed Debezium payload: {Payload}", Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error handling Debezium event.");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(options.QueueName, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("RabbitMQ consumer listening on queue {QueueName}.", options.QueueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleAsync(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var payload = root.TryGetProperty("payload", out var nestedPayload) ? nestedPayload : root;
        var operation = payload.GetProperty("op").GetString();

        if (operation != "c")
        {
            return;
        }

        var source = payload.GetProperty("source");
        var table = source.GetProperty("table").GetString();
        if (table != DatabaseTableNames.Orders)
        {
            return;
        }

        var after = payload.GetProperty("after");
        var orderId = after.GetProperty(nameof(Order.Id)).GetGuid();
        var sourceEventId = Convert.ToHexString(SHA256.HashData(body));
        logger.LogInformation("Processing order {OrderId} from Debezium event {SourceEventId}.", orderId, sourceEventId);

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var orderItems = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .ToListAsync(cancellationToken);

        if (orderItems.Count == 0)
        {
            throw new InvalidOperationException($"Order '{orderId}' has no items yet.");
        }

        logger.LogInformation("Found {OrderItemCount} items for order {OrderId}.", orderItems.Count, orderId);

        foreach (var orderItem in orderItems)
        {
            if (orderItem.Quantity < 1)
            {
                throw new InvalidOperationException("An order item quantity must be at least 1.");
            }

            var itemEventId = $"{sourceEventId}:{orderItem.ProductId}";
            if (await dbContext.StockEntries.AnyAsync(entry => entry.SourceEventId == itemEventId, cancellationToken))
            {
                logger.LogWarning(
                    "Stock entry for product {ProductId} and order {OrderId} was already processed. Skipping it.",
                    orderItem.ProductId,
                    orderId);
                continue;
            }

            var affectedProducts = await dbContext.Products
                .Where(product => product.Id == orderItem.ProductId && product.Stock >= orderItem.Quantity)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(product => product.Stock, product => product.Stock - orderItem.Quantity),
                    cancellationToken);

            if (affectedProducts == 0)
            {
                throw new InvalidOperationException($"Product '{orderItem.ProductId}' does not have enough stock.");
            }

            var stockEntry = new StockEntry
            {
                Id = Guid.NewGuid(),
                ProductId = orderItem.ProductId,
                Quantity = orderItem.Quantity,
                CreatedAt = DateTime.UtcNow,
                SourceEventId = itemEventId
            };

            dbContext.StockEntries.Add(stockEntry);
            dbContext.OrderStockEntries.Add(new OrderStockEntry
            {
                OrderId = orderId,
                StockEntry = stockEntry
            });

            logger.LogInformation(
                "Created stock entry for order {OrderId}, product {ProductId}, quantity {Quantity}.",
                orderId,
                orderItem.ProductId,
                orderItem.Quantity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Completed stock processing for order {OrderId}.", orderId);
    }
}
