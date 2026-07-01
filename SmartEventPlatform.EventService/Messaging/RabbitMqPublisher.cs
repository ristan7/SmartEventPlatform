using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace SmartEventPlatform.EventService.Messaging
{
    public interface IRabbitMqPublisher
    {
        Task PublishAsync(
            string payload, string messageId, string eventType,
            string routingKey, CancellationToken cancellationToken);
    }

    public sealed class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly PublisherRabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqPublisher(IOptions<PublisherRabbitMqOptions> options)
        {
            _options = options.Value;
            _factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };
        }

        public async Task PublishAsync(
            string payload, string messageId, string eventType,
            string routingKey, CancellationToken cancellationToken)
        {
            await EnsureInitializedAsync(cancellationToken);

            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized.");

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = messageId,
                Type = eventType,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(
                exchange: _options.Exchange, routingKey: routingKey,
                mandatory: true, basicProperties: properties,
                body: Encoding.UTF8.GetBytes(payload),
                cancellationToken: cancellationToken);
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null) return;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_channel is not null) return;

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _options.Exchange, type: ExchangeType.Direct,
                    durable: true, autoDelete: false, cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _options.DeadLetterExchange, type: ExchangeType.Direct,
                    durable: true, autoDelete: false, cancellationToken: cancellationToken);

                //Location deo
                await _channel.QueueDeclareAsync(
                    queue: _options.LocationUsageDlq,
                    durable: true, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(
                    queue: _options.LocationUsageDlq, exchange: _options.DeadLetterExchange,
                    routingKey: _options.LocationUsageRoutingKey, cancellationToken: cancellationToken);

                await _channel.QueueDeclareAsync(
                    queue: _options.LocationUsageQueue, durable: true,
                    exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", _options.DeadLetterExchange } },
                    cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(
                    queue: _options.LocationUsageQueue, exchange: _options.Exchange,
                    routingKey: _options.LocationUsageRoutingKey, cancellationToken: cancellationToken);

                //Spekaer deo
                await _channel.QueueDeclareAsync(
                    queue: _options.SpeakerUsageDlq,
                    durable: true, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(
                    queue: _options.SpeakerUsageDlq, exchange: _options.DeadLetterExchange,
                    routingKey: _options.SpeakerUsageRoutingKey, cancellationToken: cancellationToken);

                await _channel.QueueDeclareAsync(
                    queue: _options.SpeakerUsageQueue, durable: true,
                    exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", _options.DeadLetterExchange } },
                    cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(
                    queue: _options.SpeakerUsageQueue, exchange: _options.Exchange,
                    routingKey: _options.SpeakerUsageRoutingKey, cancellationToken: cancellationToken);
            }
            finally { _initLock.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
            _initLock.Dispose();
        }
    }
}