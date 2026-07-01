using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace SmartEventPlatform.RegistrationService.Messaging
{
    public interface ISagaChoreographyPublisher
    {
        Task PublishAsync(
            string routingKey,
            string payload,
            string messageType,
            CancellationToken cancellationToken);
    }


    public sealed class SagaChoreographyPublisher : ISagaChoreographyPublisher, IAsyncDisposable
    {
        private readonly SagaChoreographyRabbitMqOptions _opts;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;

        public SagaChoreographyPublisher(IOptions<SagaChoreographyRabbitMqOptions> opts)
        {
            _opts = opts.Value;
        }

        public async Task PublishAsync(
            string routingKey,
            string payload,
            string messageType,
            CancellationToken cancellationToken)
        {
            await EnsureInitializedAsync(cancellationToken);

            if (_channel is null)
                throw new InvalidOperationException("[SagaChoreo] RabbitMQ channel nije inicijaliziran.");

            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString("N"),
                Type = messageType,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(
                exchange: _opts.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: props,
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

                var factory = new ConnectionFactory
                {
                    HostName = _opts.HostName,
                    Port = _opts.Port,
                    UserName = _opts.UserName,
                    Password = _opts.Password
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _opts.Exchange, type: ExchangeType.Direct,
                    durable: true, autoDelete: false, cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _opts.DeadLetterExchange, type: ExchangeType.Direct,
                    durable: true, autoDelete: false, cancellationToken: cancellationToken);

                var dlxArgs = new Dictionary<string, object?> { { "x-dead-letter-exchange", _opts.DeadLetterExchange } };

                //EventService queue
                await _channel.QueueDeclareAsync(_opts.EventServiceDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(_opts.EventServiceDlq, _opts.DeadLetterExchange, _opts.EventServiceRoutingKey, cancellationToken: cancellationToken);
                await _channel.QueueDeclareAsync(_opts.EventServiceQueue, durable: true, exclusive: false, autoDelete: false, arguments: dlxArgs, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(_opts.EventServiceQueue, _opts.Exchange, _opts.EventServiceRoutingKey, cancellationToken: cancellationToken);

                //DirectoryService queue
                await _channel.QueueDeclareAsync(_opts.DirectoryServiceDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(_opts.DirectoryServiceDlq, _opts.DeadLetterExchange, _opts.DirectoryServiceRoutingKey, cancellationToken: cancellationToken);
                await _channel.QueueDeclareAsync(_opts.DirectoryServiceQueue, durable: true, exclusive: false, autoDelete: false, arguments: dlxArgs, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(_opts.DirectoryServiceQueue, _opts.Exchange, _opts.DirectoryServiceRoutingKey, cancellationToken: cancellationToken);

                //RegistrationService queue
                await _channel.QueueDeclareAsync(_opts.RegistrationServiceDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(_opts.RegistrationServiceDlq, _opts.DeadLetterExchange, _opts.RegistrationServiceRoutingKey, cancellationToken: cancellationToken);
                await _channel.QueueDeclareAsync(_opts.RegistrationServiceQueue, durable: true, exclusive: false, autoDelete: false, arguments: dlxArgs, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(_opts.RegistrationServiceQueue, _opts.Exchange, _opts.RegistrationServiceRoutingKey, cancellationToken: cancellationToken);
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