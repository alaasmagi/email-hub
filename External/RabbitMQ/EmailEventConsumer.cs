using Base.Message.RabbitMQ;
using Contracts.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace External.RabbitMQ;

/// <summary>
/// Queue-only consumer for the email command queue.
///
/// It does not use the shared <c>RabbitMqConsumerBase</c>: that base never surfaces
/// <see cref="BasicDeliverEventArgs.RoutingKey"/> to the handler and always nacks
/// <c>requeue: false</c>. This service needs both the routing key (for the source ↔ payload security
/// check) and per-message control over requeue (transient failures must go back on the queue). It
/// still reuses the shared <see cref="RabbitMqConnectionManager"/> and never declares, binds or
/// touches an exchange — the topology (<c>email-hub.inbox</c> bound to <c>email-hub.commands</c>) is
/// owned outside this application.
/// </summary>
public class EmailEventConsumer : BackgroundService
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly EmailQueueOptions _queueOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailEventConsumer> _logger;

    public EmailEventConsumer(
        RabbitMqConnectionManager connectionManager,
        EmailQueueOptions queueOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailEventConsumer> logger)
    {
        _connectionManager = connectionManager;
        _queueOptions = queueOptions;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connectionManager.CreateChannelAsync(cancellationToken: stoppingToken);
        try
        {
            // Bounded in-flight deliveries. Manual ack (autoAck: false) below.
            await channel.BasicQosAsync(0, _queueOptions.PrefetchCount, global: false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) => OnReceivedAsync(channel, ea, stoppingToken);

            // No QueueDeclare / ExchangeDeclare / QueueBind — the queue already exists.
            await channel.BasicConsumeAsync(
                _queueOptions.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Consuming email commands from queue '{Queue}' (prefetch {Prefetch}).",
                _queueOptions.QueueName, _queueOptions.PrefetchCount);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            await channel.DisposeAsync();
        }
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IEmailCommandProcessor>();

            var disposition = await processor.ProcessAsync(ea.RoutingKey, ea.Body, stoppingToken);

            switch (disposition)
            {
                case MessageDisposition.Ack:
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    break;

                case MessageDisposition.RequeueTransient:
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                    break;

                case MessageDisposition.RejectNoRequeue:
                default:
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                    break;
            }
        }
        catch (Exception exception)
        {
            // Unexpected failure (e.g. transient infra like the DB being unreachable). Requeue and let
            // the queue's x-delivery-limit dead-letter it after a few attempts — no app-side loop. The
            // idempotency guard makes a redelivery after a successful-but-unacked send a safe no-op.
            // Only the routing key is logged; never the body.
            _logger.LogError(
                exception,
                "Unexpected error processing delivery; requeueing. RoutingKey: {RoutingKey}", ea.RoutingKey);

            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }
}
