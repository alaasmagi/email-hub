namespace External.RabbitMQ;

/// <summary>
/// Queue-only consumer settings. The queue name is provided by configuration (env) rather than being
/// hard-coded, so the queue this service reads can change without a rebuild.
/// </summary>
public class EmailQueueOptions
{
    public string QueueName { get; set; } = default!;
}

