namespace External.RabbitMQ;

/// <summary>
/// Queue-only consumer settings. The queue name comes from configuration (env) rather than being
/// hard-coded, so the queue this service reads can change without a rebuild. This service never
/// declares or binds the queue — the topology is owned outside the application.
/// </summary>
public class EmailQueueOptions
{
    public string QueueName { get; set; } = default!;

    /// <summary>
    /// Unacked-message limit per consumer (AMQP basic.qos). Bounds how many deliveries are in flight
    /// so one slow consumer cannot pull the whole queue into memory. Must be &gt; 0.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;
}
