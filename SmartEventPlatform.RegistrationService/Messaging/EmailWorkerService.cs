using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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

            await _channel.QueueDeclareAsync(
                queue: mq.Queue,
                durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0, prefetchCount: 1,
                global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleEmailAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: mq.Queue, autoAck: false,
                consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation(
                "EmailWorkerService started. Queue='{Queue}', MaxPerMin={Limit}, Folder='{Folder}'.",
                mq.Queue, mq.MaxEmailsPerMinute, mq.OutboxFolder);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleEmailAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;
            var mq = _options.Value;

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
                await RecordEmailSentAsync();

                _logger.LogInformation(
                    "Email sent. RegistrationId={Id}, Recipient={Email}.",
                    message.RegistrationId, message.ParticipantEmail);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailWorker: error.");
                if (_channel is not null)
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: true,
                        cancellationToken: cancellationToken);
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

        private async Task RecordEmailSentAsync()
        {
            await _rateLimiterLock.WaitAsync();
            try { _sentTimestamps.Enqueue(DateTime.UtcNow); }
            finally { _rateLimiterLock.Release(); }
        }

        private static async Task SaveEmailToFileAsync(
            EmailNotificationMessage msg, string folder, CancellationToken ct)
        {
            var fileName = $"email_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_reg{msg.RegistrationId}.txt";
            var content = $"""
                To: {msg.ParticipantEmail}
                Subject: Potvrda registracije — {msg.EventName}
                Date: {DateTime.UtcNow:R}

                Postovani/a {msg.ParticipantFirstName} {msg.ParticipantLastName},

                Uspjesno ste se registrovali/e za sljedeci dogadjaj:

                  Naziv: {msg.EventName}
                  Datum i vrijeme: {msg.EventDateTime:dd.MM.yyyy HH:mm}
                  ID registracije: {msg.RegistrationId}
                  Datum registracije: {msg.RegistrationDate:dd.MM.yyyy}

                S postovanjem,
                SmartEvent platforma
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