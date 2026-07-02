using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace SmartEventPlatform.RegistrationService.Messaging
{
    public sealed class EmailWorkerService : BackgroundService
    {
        private readonly IOptions<EmailRabbitMqOptions> _options;
        private readonly ILogger<EmailWorkerService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly Queue<DateTime> _sentTimestamps = new();
        private readonly SemaphoreSlim _rateLimiterLock = new(1, 1);
        private readonly ConcurrentDictionary<string, int> _retryCounts = new();

        public EmailWorkerService(
            IOptions<EmailRabbitMqOptions> options,
            ILogger<EmailWorkerService> logger)
        {
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mq = _options.Value;
            Directory.CreateDirectory(mq.OutboxFolder);

            var factory = new ConnectionFactory
            {
                HostName = mq.HostName,
                Port = mq.Port,
                UserName = mq.UserName,
                Password = mq.Password
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: mq.Exchange, type: ExchangeType.Direct,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: mq.DeadLetterExchange, type: ExchangeType.Direct,
                durable: true, autoDelete: false, cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: mq.DeadLetterQueue,
                durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: mq.DeadLetterQueue, exchange: mq.DeadLetterExchange,
                routingKey: mq.RoutingKey, cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: mq.Queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { { "x-dead-letter-exchange", mq.DeadLetterExchange } },
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: mq.Queue, exchange: mq.Exchange,
                routingKey: mq.RoutingKey, cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0, prefetchCount: mq.PrefetchCount,
                global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleEmailAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: mq.Queue, autoAck: false,
                consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation(
                "EmailWorkerService started. Queue='{Queue}', DLQ='{DLQ}', MaxRetries={Max}, MaxPerMin={Limit}, Folder='{Folder}'.",
                mq.Queue, mq.DeadLetterQueue, mq.MaxRetryCount, mq.MaxEmailsPerMinute, mq.OutboxFolder);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleEmailAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;
            var mq = _options.Value;
            var messageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString();

            try
            {
                var message = JsonSerializer.Deserialize<EmailNotificationMessage>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()));

                if (message is null)
                {
                    _logger.LogWarning("EmailWorker: could not deserialize. ACK-ing.");
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                    return;
                }

                await WaitForRateLimitSlotAsync(mq.MaxEmailsPerMinute, cancellationToken);

                await SaveEmailToFileAsync(message, mq.OutboxFolder, cancellationToken);
                await RecordEmailSentAsync(cancellationToken);

                _logger.LogInformation(
                    "Email sent. RegistrationId={Id}, Recipient={Email}.",
                    message.RegistrationId, message.ParticipantEmail);

                _retryCounts.TryRemove(messageId, out int _);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                var retryCount = _retryCounts.AddOrUpdate(messageId, 1, (_, old) => old + 1);

                _logger.LogError(ex,
                    "EmailWorker: error processing message. MessageId={Id}, Attempt={N}/{Max}.",
                    messageId, retryCount, mq.MaxRetryCount);

                if (_channel is null) return;

                if (retryCount >= mq.MaxRetryCount)
                {
                    
                    _retryCounts.TryRemove(messageId, out int _);
                    _logger.LogWarning(
                        "EmailWorker: max retries exceeded, dead-lettering. MessageId={Id}.", messageId);
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: false,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: true,
                        cancellationToken: cancellationToken);
                }
            }
        }

        private async Task WaitForRateLimitSlotAsync(int maxPerMinute, CancellationToken cancellationToken)
        {
            while (true)
            {
                TimeSpan? waitTime = null;

                await _rateLimiterLock.WaitAsync(cancellationToken);
                try
                {
                    var now = DateTime.UtcNow;
                    var windowStart = now.AddMinutes(-1);

                    //izbacujemo najstarija vremena koja su iznad 60 sek
                    while (_sentTimestamps.Count > 0 && _sentTimestamps.Peek() < windowStart)
                        _sentTimestamps.Dequeue();

                    if (_sentTimestamps.Count < maxPerMinute)
                        return;  // ima mesta

                    var oldest = _sentTimestamps.Peek();
                    waitTime = oldest.AddMinutes(1) - now;

                    _logger.LogInformation(
                        "EmailWorker: rate limit reached ({Count}/{Max}/min). Waiting {Wait:F1}s.",
                        _sentTimestamps.Count, maxPerMinute, waitTime.Value.TotalSeconds);
                }
                finally { _rateLimiterLock.Release(); }

                if (waitTime.HasValue && waitTime.Value > TimeSpan.Zero)
                    await Task.Delay(waitTime.Value, cancellationToken);
            }
        }

        private async Task RecordEmailSentAsync(CancellationToken cancellationToken)
        {
            await _rateLimiterLock.WaitAsync(cancellationToken);
            try { _sentTimestamps.Enqueue(DateTime.UtcNow); }
            finally { _rateLimiterLock.Release(); }
        }

        private static async Task SaveEmailToFileAsync(
            EmailNotificationMessage msg, string folder, CancellationToken ct)
        {
            var fileName = $"email_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_reg{msg.RegistrationId}.txt";
            var content = $"""
                To: {msg.ParticipantEmail}
                Subject: Registration Confirmation — {msg.EventName}
                Date: {DateTime.UtcNow:R}

                Dear {msg.ParticipantFirstName} {msg.ParticipantLastName},

                You have successfully registered for the following event:

                  Event: {msg.EventName}
                  Date and time: {msg.EventDateTime:dd.MM.yyyy HH:mm}
                  Registration ID: {msg.RegistrationId}
                  Registration date: {msg.RegistrationDate:dd.MM.yyyy}

                Best regards,
                SmartEvent Platform
                """;

            await File.WriteAllTextAsync(Path.Combine(folder, fileName), content, ct);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _rateLimiterLock.Dispose();
            base.Dispose();
        }
    }
}