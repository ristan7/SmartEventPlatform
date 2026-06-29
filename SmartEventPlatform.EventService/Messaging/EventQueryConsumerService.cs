using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.EventService.Data;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.EventService.Messaging
{
    /// <summary>
    /// Server strana Request-Reply obrasca.
    /// Slusa zahtjeve RegistrationService-a na RequestQueue,
    /// dohvata event iz baze i salje odgovor na ReplyTo adresu
    /// iz zaglavlja poruke s istim CorrelationId-om.
    /// </summary>
    public sealed class EventQueryConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<EventQueryRabbitMqOptions> _options;
        private readonly ILogger<EventQueryConsumerService> _logger;
        private IConnection? _connection;
        private IChannel? _consumerChannel;
        private IChannel? _publishChannel;

        public EventQueryConsumerService(
            IServiceScopeFactory scopeFactory,
            IOptions<EventQueryRabbitMqOptions> options,
            ILogger<EventQueryConsumerService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mq = _options.Value;

            var factory = new ConnectionFactory
            {
                HostName = mq.HostName,
                Port = mq.Port,
                UserName = mq.UserName,
                Password = mq.Password
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            // Dva odvojena kanala: jedan za consume, drugi za publish odgovora
            _consumerChannel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            _publishChannel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // durable=false: kratkotrajna query infrastruktura;
            // ako EventService restartuje, RegistrationService dobija timeout i pada na HTTP fallback
            await _consumerChannel.QueueDeclareAsync(
                queue: mq.RequestQueue,
                durable: false, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: stoppingToken);

            await _consumerChannel.BasicQosAsync(
                prefetchSize: 0, prefetchCount: 1,
                global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.ReceivedAsync += async (_, ea) => await HandleRequestAsync(ea, stoppingToken);

            await _consumerChannel.BasicConsumeAsync(
                queue: mq.RequestQueue, autoAck: false,
                consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation(
                "EventQueryConsumerService started. Listening on '{Queue}'.", mq.RequestQueue);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleRequestAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_consumerChannel is null || _publishChannel is null) return;

            var correlationId = ea.BasicProperties.CorrelationId;
            var replyTo = ea.BasicProperties.ReplyTo;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<EventInfoRequest>(body);

                if (request is null || string.IsNullOrEmpty(replyTo) || string.IsNullOrEmpty(correlationId))
                {
                    _logger.LogWarning("EventQuery: invalid request — missing body, ReplyTo or CorrelationId.");
                    await _consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                    return;
                }

                _logger.LogInformation(
                    "EventQuery: request received. EventId={EventId}, CorrelationId={CorrelationId}.",
                    request.EventId, correlationId);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

                var evt = await db.Events
                    .FirstOrDefaultAsync(e => e.EventId == request.EventId, cancellationToken);

                var reply = evt is not null
                    ? new EventInfoReply
                    {
                        EventId = evt.EventId,
                        Exists = true,
                        EventName = evt.EventName,
                        Capacity = evt.LocationCapacitySnapshot
                    }
                    : new EventInfoReply { EventId = request.EventId, Exists = false };

                var replyBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reply));
                var replyProps = new BasicProperties
                {
                    CorrelationId = correlationId,
                    ContentType = "application/json"
                };

                // Saljemo odgovor na ReplyTo queue koji je postavio RegistrationService
                await _publishChannel.BasicPublishAsync(
                    exchange: string.Empty,  // default exchange — direktno po imenu queue-a
                    routingKey: replyTo,
                    mandatory: false,
                    basicProperties: replyProps,
                    body: replyBody,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "EventQuery: reply sent. EventId={EventId}, Exists={Exists}, CorrelationId={CorrelationId}.",
                    reply.EventId, reply.Exists, correlationId);

                await _consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventQuery: error. CorrelationId={CorrelationId}.", correlationId);
                if (_consumerChannel is not null)
                    await _consumerChannel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: true,
                        cancellationToken: cancellationToken);
            }
        }

        public override void Dispose()
        {
            _consumerChannel?.Dispose();
            _publishChannel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}