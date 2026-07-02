using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.RegistrationService.Messaging
{
    public interface IEmailQueuePublisher
    {
        Task EnqueueAsync(EmailNotificationMessage message, CancellationToken cancellationToken);
    }

    public sealed class EmailQueuePublisher : IEmailQueuePublisher, IAsyncDisposable
    {
        private readonly EmailRabbitMqOptions _options;
        private readonly ILogger<EmailQueuePublisher> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;

        public EmailQueuePublisher(
            IOptions<EmailRabbitMqOptions> options,
            ILogger<EmailQueuePublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task EnqueueAsync(EmailNotificationMessage message, CancellationToken cancellationToken)
        {
            await EnsureInitializedAsync(cancellationToken);

            if (_channel is null) return;

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString("N")
            };

            await _channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: _options.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Email enqueued. RegistrationId={Id}, Recipient={Email}.",
                message.RegistrationId, message.ParticipantEmail);
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null) return;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_channel is not null) return;

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _options.Exchange, type: ExchangeType.Direct,
                    durable: true, autoDelete: false, cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _options.DeadLetterExchange, type: ExchangeType.Direct,
                    durable: true, autoDelete: false, cancellationToken: cancellationToken);

                await _channel.QueueDeclareAsync(
                    queue: _options.Queue, durable: true, exclusive: false, autoDelete: false,
                    arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", _options.DeadLetterExchange } },
                    cancellationToken: cancellationToken);

                await _channel.QueueBindAsync(
                    queue: _options.Queue, exchange: _options.Exchange,
                    routingKey: _options.RoutingKey, cancellationToken: cancellationToken);
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