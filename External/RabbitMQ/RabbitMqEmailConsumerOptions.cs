namespace External.RabbitMQ;

public class RabbitMqEmailConsumerOptions
{
    public string? Uri { get; set; }
    public string[] ExchangeNames { get; set; } = ["identity-events"];
    public string QueueName { get; set; } = "email-hub.identity-email";
    public ushort PrefetchCount { get; set; } = 10;
    public int RetryDelaySeconds { get; set; } = 10;
    public string[] RoutingKeys { get; set; } =
    [
        "email.password-reset",
        "email.verify",
        "email.2fa-otp"
    ];
}
