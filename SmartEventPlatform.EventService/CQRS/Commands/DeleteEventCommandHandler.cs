using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.EventService.CQRS.Repositories;
using SmartEventPlatform.EventService.Messaging;
using System.Text.Json;

namespace SmartEventPlatform.EventService.CQRS.Commands
{
    public class DeleteEventCommandHandler
    {
        private readonly IEventWriteRepository _writeRepository;
        private readonly PublisherRabbitMqOptions _publisherOptions;
        private readonly ILogger<DeleteEventCommandHandler> _logger;

        public DeleteEventCommandHandler(
            IEventWriteRepository writeRepository,
            Microsoft.Extensions.Options.IOptions<PublisherRabbitMqOptions> publisherOptions,
            ILogger<DeleteEventCommandHandler> logger)
        {
            _writeRepository = writeRepository;
            _publisherOptions = publisherOptions.Value;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteEventCommand command)
        {
            var existingEvent = await _writeRepository.GetByIdForWriteAsync(command.EventId);
            if (existingEvent == null)
                return false;

            if (existingEvent.EventSpeakers.Any())
                throw new InvalidOperationException(
                    "This event cannot be deleted because it has assigned speakers.");

            if (await _writeRepository.HasRegistrationsAsync(command.EventId))
                throw new InvalidOperationException(
                    "This event cannot be deleted because it has participant registrations.");

            var locationNotification = new OutboxMessage
            {
                EventType = nameof(EventDeletedEvent),
                RoutingKey = _publisherOptions.LocationUsageRoutingKey,
                Payload = JsonSerializer.Serialize(new EventDeletedEvent
                {
                    EventId = command.EventId,
                    LocationId = existingEvent.LocationId
                })
            };

            await _writeRepository.DeleteAsync(existingEvent, locationNotification);

            _logger.LogInformation(
                "CQRS DeleteEventCommand handled. EventId={EventId}, LocationId={LocationId}.",
                command.EventId, existingEvent.LocationId);

            return true;
        }
    }
}