using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartEventPlatform.Contracts.Integration;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.RegistrationService.Messaging
{
    public interface IRabbitMqEventQueryClient
    {
        Task<EventInfoReply?> QueryEventInfoAsync(long eventId, CancellationToken cancellationToken);
    }

    public sealed class RabbitMqEventQueryClient : IRabbitMqEventQueryClient, IAsyncDisposable
    {
        private readonly EventQueryRabbitMqOptions _options;
        private readonly ILogger<RabbitMqEventQueryClient> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private IConnection? _connection;
        private IChannel? _publishChannel;
        private IChannel? _consumerChannel;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<EventInfoReply>>
            _pendingRequests = new();

        public RabbitMqEventQueryClient(
            IOptions<EventQueryRabbitMqOptions> options,
            ILogger<RabbitMqEventQueryClient> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EventInfoReply?> QueryEventInfoAsync(long eventId, CancellationToken cancellationToken)
        {
            await EnsureInitializedAsync(cancellationToken);

            if (_publishChannel is null) return null;

            var correlationId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<EventInfoReply>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[correlationId] = tcs;

            try
            {
                var body = Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(new EventInfoRequest { EventId = eventId }));

                var properties = new BasicProperties
                {
                    CorrelationId = correlationId,
                    ReplyTo = _options.ReplyQueue,
                    ContentType = "application/json"
                };

                await _publishChannel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: _options.RequestQueue,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "EventQuery: request sent. EventId={Id}, CorrelationId={Cid}.", eventId, correlationId);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                await using var reg = timeoutCts.Token.Register(() =>
                {
                    if (_pendingRequests.TryRemove(correlationId, out var t))
                        t.TrySetCanceled();
                });

                return await tcs.Task;//ceka se dok se ne desi odgovor
            }
            catch (OperationCanceledException)
            {
                _pendingRequests.TryRemove(correlationId, out _);
                _logger.LogWarning(
                    "EventQuery: timeout. EventId={Id}, CorrelationId={Cid}.", eventId, correlationId);
                return null;
            }
            catch (Exception ex)
            {
                _pendingRequests.TryRemove(correlationId, out _);
                _logger.LogError(ex, "EventQuery: error sending request. EventId={Id}.", eventId);
                return null;
            }
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_publishChannel is not null) return;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_publishChannel is not null) return;

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _publishChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                _consumerChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await _publishChannel.QueueDeclareAsync(
                    queue: _options.RequestQueue,
                    durable: false, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);

                await _consumerChannel.QueueDeclareAsync(
                    queue: _options.ReplyQueue,
                    durable: false, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);

                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.ReceivedAsync += HandleReplyAsync;

                await _consumerChannel.BasicConsumeAsync(
                    queue: _options.ReplyQueue, autoAck: false,
                    consumer: consumer, cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "RabbitMqEventQueryClient initialized. Request='{Req}', Reply='{Rep}'.",
                    _options.RequestQueue, _options.ReplyQueue);
            }
            finally { _initLock.Release(); }
        }

        private async Task HandleReplyAsync(object sender, BasicDeliverEventArgs ea)
        {
            if (_consumerChannel is null) return;

            try
            {
                var correlationId = ea.BasicProperties.CorrelationId;

                if (string.IsNullOrEmpty(correlationId) ||
                    !_pendingRequests.TryRemove(correlationId, out var tcs))
                {
                    // Odgovor koji je stigao nakon timeoutа — samo ACK
                    _logger.LogWarning(
                        "EventQuery: reply with unknown CorrelationId='{Cid}'. Ignoring.", correlationId);
                    await _consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                var reply = JsonSerializer.Deserialize<EventInfoReply>(ea.Body.ToArray());
                if (reply is not null)
                {
                    tcs.TrySetResult(reply);
                    _logger.LogInformation(
                        "EventQuery: reply received. EventId={Id}, Exists={E}, CorrelationId={Cid}.",
                        reply.EventId, reply.Exists, correlationId);
                }
                else
                    tcs.TrySetException(new InvalidOperationException(
                        $"Could not deserialize EventInfoReply. CorrelationId={correlationId}"));

                await _consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventQuery: error handling reply.");
                if (_consumerChannel is not null)
                    await _consumerChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_consumerChannel is not null) await _consumerChannel.DisposeAsync();
            if (_publishChannel is not null) await _publishChannel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
            _initLock.Dispose();
        }
    }
}