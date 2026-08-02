using System.ComponentModel.DataAnnotations;

namespace OrdersPipeline.Api.Infrastructure.Messaging;

internal sealed class RabbitMqOptions
{
    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required string ExchangeName { get; init; }

    [Required]
    public required string QueueName { get; init; }

    [Required]
    public required string RoutingKey { get; init; }
}
