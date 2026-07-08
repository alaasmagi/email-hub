using System.Text.Json;
using Contracts.Application;
using Contracts.External.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace External.RabbitMQ;

public class KeycloakEmailEventConsumer : BackgroundService, IKeycloakEmailEventConsumer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeycloakEmailEventConsumer> _logger;
    private readonly RabbitMqEmailConsumerOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    public KeycloakEmailEventConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqEmailConsumerOptions> options,
        ILogger<KeycloakEmailEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumerAsync(stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    "RabbitMQ email consumer failed to connect to {Endpoint}. Retrying in {RetryDelaySeconds} seconds. Error: {ErrorMessage}",
                    GetEndpointDescription(),
                    _options.RetryDelaySeconds,
                    exception.Message);

                _logger.LogDebug(exception, "RabbitMQ email consumer startup failure details.");

                if (LooksLikeHttpUrl(_options.Uri))
                {
                    _logger.LogWarning(
                        "RABBITMQ_URI must be an AMQP endpoint, not an HTTP management URL. Use amqp:// or amqps://.");
                }

                _logger.LogInformation(
                    "Next RabbitMQ retry in {RetryDelaySeconds} seconds.",
                    _options.RetryDelaySeconds);

                await CloseConnectionAsync(CancellationToken.None);

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    private async Task StartConsumerAsync(CancellationToken stoppingToken)
    {
        var factory = CreateConnectionFactory();

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        foreach (var exchangeName in _options.ExchangeNames)
        {
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            foreach (var routingKey in _options.RoutingKeys)
            {
                await _channel.QueueBindAsync(
                    queue: _options.QueueName,
                    exchange: exchangeName,
                    routingKey: routingKey,
                    cancellationToken: stoppingToken);
            }
        }

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleMessageAsync;

        await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "RabbitMQ email consumer started. Exchanges: {ExchangeNames}, queue: {QueueName}",
            string.Join(", ", _options.ExchangeNames),
            _options.QueueName);
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        var uri = new Uri(_options.Uri!);
        var factory = new ConnectionFactory
        {
            Uri = uri,
            AutomaticRecoveryEnabled = true
        };

        if (uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
        {
            factory.Ssl.Enabled = true;
            factory.Ssl.ServerName = uri.Host;
        }

        return factory;
    }

    private string GetEndpointDescription()
    {
        var uri = new Uri(_options.Uri!);
        return $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}";
    }

    private static bool LooksLikeHttpUrl(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ email consumer stopping.");

        await CloseConnectionAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }

    private async Task CloseConnectionAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection = null;
        }
    }

    private async Task HandleMessageAsync(
        object sender,
        BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var emailEvent = JsonSerializer.Deserialize<KeycloakEmailEvent>(
                eventArgs.Body.Span,
                JsonSerializerOptions);

            if (emailEvent is null)
            {
                _logger.LogWarning("Received empty or invalid Keycloak email event.");
                await _channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);
                return;
            }

            _logger.LogInformation(
                "Received Keycloak email event. EventType: {EventType}, RealmName: {RealmName}, DeliveryTag: {DeliveryTag}",
                emailEvent.EventType,
                emailEvent.RealmName,
                eventArgs.DeliveryTag);

            using var scope = _scopeFactory.CreateScope();

            var mapper = scope.ServiceProvider.GetRequiredService<IKeycloakEmailEventMapper>();
            var dispatchService = scope.ServiceProvider.GetRequiredService<IEmailDispatchService>();

            var mapResponse = mapper.Map(emailEvent);
            if (!mapResponse.Successful || mapResponse.Value is null)
            {
                _logger.LogWarning(
                    "Failed to map Keycloak email event. EventType: {EventType}, RealmName: {RealmName}, DeliveryTag: {DeliveryTag}",
                    emailEvent.EventType,
                    emailEvent.RealmName,
                    eventArgs.DeliveryTag);

                await _channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);
                return;
            }

            var dispatchResponse = await dispatchService.SendTemplatedEmailAsync(
                mapResponse.Value,
                CancellationToken.None);

            if (!dispatchResponse.Successful)
            {
                _logger.LogWarning(
                    "Failed to dispatch Keycloak email event. EventType: {EventType}, RealmName: {RealmName}, DeliveryTag: {DeliveryTag}. Message will be requeued.",
                    emailEvent.EventType,
                    emailEvent.RealmName,
                    eventArgs.DeliveryTag);

                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
                return;
            }

            await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);

            _logger.LogInformation(
                "Acknowledged Keycloak email event. EventType: {EventType}, RealmName: {RealmName}, DeliveryTag: {DeliveryTag}",
                emailEvent.EventType,
                emailEvent.RealmName,
                eventArgs.DeliveryTag);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Failed to deserialize Keycloak email event.");
            await _channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected failure while processing Keycloak email event.");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }
}
