namespace OrdersPipeline.Api.Infrastructure.Messaging;

internal static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqDebeziumConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetRequiredSection("RabbitMQ"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<RabbitMqDebeziumConsumer>();

        return services;
    }
}
