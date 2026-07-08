namespace External.RabbitMQ;

public class RabbitMqEmailConsumerOptions
{
    public bool Enabled { get; set; } = true;
    public string? Uri { get; set; }
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public bool UseTls { get; set; }
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
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
