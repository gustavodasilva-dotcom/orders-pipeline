using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersPipeline.Api.Infrastructure.Messaging;

internal sealed class RabbitMqDebeziumConsumer(
    IOptions<RabbitMqOptions> optionsAccessor,
    ILogger<RabbitMqDebeziumConsumer> logger) : BackgroundService
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
                var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                logger.LogInformation(
                    "Received Debezium event from {Exchange} with routing key {RoutingKey}: {Payload}",
                    options.ExchangeName,
                    eventArgs.RoutingKey,
                    payload);

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error processing Debezium event.");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(options.QueueName, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("RabbitMQ consumer listening on queue {QueueName}.", options.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
