using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.EventService.Clients;
using SmartEventPlatform.EventService.CQRS.Repositories;
using SmartEventPlatform.EventService.Messaging;
using System.Text.Json;

namespace SmartEventPlatform.EventService.CQRS.Commands
{
    public class UpdateEventCommandHandler
    {
        private readonly IEventWriteRepository _writeRepository;
        private readonly IDirectoryServiceClient _directoryServiceClient;
        private readonly PublisherRabbitMqOptions _publisherOptions;
        private readonly ILogger<UpdateEventCommandHandler> _logger;

        public UpdateEventCommandHandler(
            IEventWriteRepository writeRepository,
            IDirectoryServiceClient directoryServiceClient,
            Microsoft.Extensions.Options.IOptions<PublisherRabbitMqOptions> publisherOptions,
            ILogger<UpdateEventCommandHandler> logger)
        {
            _writeRepository = writeRepository;
            _directoryServiceClient = directoryServiceClient;
            _publisherOptions = publisherOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Vraća false ako event nije pronađen.
        /// Baca InvalidOperationException ako validacija ne prođe.
        /// </summary>
        public async Task<bool> Handle(UpdateEventCommand command)
        {
            // Učitavamo tracked entitet za write (ne koristimo read repozitorij!)
            var existingEvent = await _writeRepository.GetByIdForWriteAsync(command.EventId);
            if (existingEvent == null)
                return false;

            // Validacija: nova lokacija mora postojati
            var location = await _directoryServiceClient.GetLocationAsync(command.LocationId);
            if (location == null)
                throw new InvalidOperationException("Selected location does not exist.");

            // Validacija: tip događaja mora postojati
            if (!await _writeRepository.EventTypeExistsAsync(command.EventTypeId))
                throw new InvalidOperationException("Selected event type does not exist.");

            var oldLocationId = existingEvent.LocationId;

            // Ažuriramo polja tracked entiteta
            existingEvent.EventName = command.EventName;
            existingEvent.Agenda = command.Agenda;
            existingEvent.EventDateTime = command.EventDateTime;
            existingEvent.DurationInMinutes = command.DurationInMinutes;
            existingEvent.RegistrationFee = command.RegistrationFee;
            existingEvent.LocationId = command.LocationId;
            existingEvent.LocationNameSnapshot = location.LocationName;
            existingEvent.LocationAddressSnapshot = location.Address;
            existingEvent.LocationCapacitySnapshot = location.Capacity;
            existingEvent.EventTypeId = command.EventTypeId;

            // Gradimo Outbox poruke — ako se lokacija promijenila, šaljemo dva eventa
            var outboxMessages = new List<OutboxMessage>();

            if (oldLocationId != command.LocationId)
            {
                outboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(EventDeletedEvent),
                    RoutingKey = _publisherOptions.LocationUsageRoutingKey,
                    Payload = JsonSerializer.Serialize(new EventDeletedEvent
                    {
                        EventId = command.EventId,
                        LocationId = oldLocationId
                    })
                });

                outboxMessages.Add(new OutboxMessage
                {
                    EventType = nameof(EventCreatedEvent),
                    RoutingKey = _publisherOptions.LocationUsageRoutingKey,
                    Payload = JsonSerializer.Serialize(new EventCreatedEvent
                    {
                        EventId = command.EventId,
                        LocationId = command.LocationId
                    })
                });
            }

            await _writeRepository.UpdateAsync(existingEvent, outboxMessages);

            _logger.LogInformation(
                "CQRS UpdateEventCommand handled. EventId={EventId}, LocationChanged={Changed}.",
                command.EventId, oldLocationId != command.LocationId);

            return true;
        }
    }
}