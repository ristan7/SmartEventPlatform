using SmartEventPlatform.Contracts.Integration;
using SmartEventPlatform.EventService.Clients;
using SmartEventPlatform.EventService.CQRS.Repositories;
using SmartEventPlatform.EventService.Messaging;
using SmartEventPlatform.EventService.Models;
using System.Text.Json;

namespace SmartEventPlatform.EventService.CQRS.Commands
{
    /// <summary>
    /// Handler za CreateEventCommand.
    /// Ručna implementacija — controller direktno poziva Handle() metodu.
    /// </summary>
    public class CreateEventCommandHandler
    {
        private readonly IEventWriteRepository _writeRepository;
        private readonly IDirectoryServiceClient _directoryServiceClient;
        private readonly PublisherRabbitMqOptions _publisherOptions;
        private readonly ILogger<CreateEventCommandHandler> _logger;

        public CreateEventCommandHandler(
            IEventWriteRepository writeRepository,
            IDirectoryServiceClient directoryServiceClient,
            Microsoft.Extensions.Options.IOptions<PublisherRabbitMqOptions> publisherOptions,
            ILogger<CreateEventCommandHandler> logger)
        {
            _writeRepository = writeRepository;
            _directoryServiceClient = directoryServiceClient;
            _publisherOptions = publisherOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Izvršava komandu kreiranja događaja.
        /// Vraća ID kreiranog događaja.
        /// Baca InvalidOperationException ako validacija ne prođe.
        /// </summary>
        public async Task<long> Handle(CreateEventCommand command)
        {
            // Validacija: lokacija mora postojati u DirectoryService-u
            var location = await _directoryServiceClient.GetLocationAsync(command.LocationId);
            if (location == null)
                throw new InvalidOperationException("Selected location does not exist.");

            // Validacija: tip događaja mora postojati
            if (!await _writeRepository.EventTypeExistsAsync(command.EventTypeId))
                throw new InvalidOperationException("Selected event type does not exist.");

            // Gradimo write entitet — ne ReadModel!
            var newEvent = new Event
            {
                EventName = command.EventName,
                Agenda = command.Agenda,
                EventDateTime = command.EventDateTime,
                DurationInMinutes = command.DurationInMinutes,
                RegistrationFee = command.RegistrationFee,
                LocationId = command.LocationId,
                LocationNameSnapshot = location.LocationName,
                LocationAddressSnapshot = location.Address,
                LocationCapacitySnapshot = location.Capacity,
                EventTypeId = command.EventTypeId
            };

            // Outbox factory: prima pravi EventId (tek nakon SaveChanges) i kreira poruku
            var routingKey = _publisherOptions.LocationUsageRoutingKey;
            var locationId = command.LocationId;

            var newId = await _writeRepository.CreateAsync(newEvent, eventId =>
                new OutboxMessage
                {
                    EventType = nameof(EventCreatedEvent),
                    RoutingKey = routingKey,
                    Payload = JsonSerializer.Serialize(new EventCreatedEvent
                    {
                        EventId = eventId,
                        LocationId = locationId
                    })
                });

            _logger.LogInformation(
                "CQRS CreateEventCommand handled. EventId={EventId}, LocationId={LocationId}.",
                newId, command.LocationId);

            return newId;
        }
    }
}