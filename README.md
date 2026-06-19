# Smart Event Platform

Smart Event Platform is a microservice-based application for managing professional events, speakers, locations, participants, and participant registrations.

The application was originally developed as a monolithic ASP.NET Core MVC application and was later decomposed into multiple backend services.

## Architecture

The solution is implemented using a microservice-based architecture.

The system is decomposed into the following projects:

* `SmartEventPlatform.EventService`
* `SmartEventPlatform.DirectoryService`
* `SmartEventPlatform.RegistrationService`
* `SmartEventPlatformWeb`
* `SmartEventPlatform.Contracts`

Each backend service owns its own domain logic and database. The MVC frontend does not access service databases directly. Instead, it communicates with backend services through HTTP clients.

## Service Decomposition

### EventService

`EventService` is responsible for the event organization domain.

It manages:

* events
* event types
* event-speaker assignments
* event catalogue data
* event-related validation rules

Events represent the central part of the event organization process. This service stores event data and references external entities, such as locations and speakers, by their identifiers.

`EventService` does not directly own locations, speakers, participants, or registrations.

### DirectoryService

`DirectoryService` is responsible for the directory/catalogue domain.

It manages:

* locations
* speakers

Locations and speakers are separated from `EventService` because they are independent entities with their own lifecycle. A location or speaker can exist independently of a specific event and can be reused across multiple events.

This separation makes the system more aligned with microservice principles because each service owns a clearly defined business responsibility.

### RegistrationService

`RegistrationService` is responsible for the participant registration domain.

It manages:

* participants
* registrations
* available events calculation
* registration-related validation rules

Registrations are separated into their own service because participant registration is a different business process from event organization.

`RegistrationService` stores only the `EventId` as a reference to an event owned by `EventService`.

### SmartEventPlatformWeb

`SmartEventPlatformWeb` is the ASP.NET Core MVC frontend application.

It is responsible for:

* rendering Razor views
* handling user interaction
* calling backend services through HTTP clients
* displaying validation and error messages to the user

The frontend does not access service databases directly.

### Contracts

`SmartEventPlatform.Contracts` contains shared DTO classes used for communication between the frontend and backend services.

This project is used to standardize request and response models between services and avoid duplicating contract classes across projects.

## Inter-Service Communication

The system uses both synchronous HTTP communication and asynchronous messaging.

Synchronous HTTP communication is used when the caller needs an immediate answer in order to complete the current user operation.

Asynchronous messaging is used when one service needs to inform another service that something has changed, without blocking the original operation.

## Synchronous HTTP Communication

The main synchronous communication flows are:

* `SmartEventPlatformWeb -> EventService`
* `SmartEventPlatformWeb -> DirectoryService`
* `SmartEventPlatformWeb -> RegistrationService`
* `RegistrationService -> EventService`
* `EventService -> DirectoryService`
* `EventService -> RegistrationService`

### RegistrationService -> EventService

`RegistrationService` communicates with `EventService` when creating or editing registrations.

This communication is necessary because `RegistrationService` must immediately verify that:

* the selected event exists
* the event is available for registration
* the event location capacity has not been reached

This is implemented synchronously because the user cannot complete a registration unless this information is known immediately.

### EventService -> RegistrationService

`EventService` communicates with `RegistrationService` before deleting an event.

This communication is necessary because an event that already has participant registrations must not be deleted.

### EventService -> DirectoryService

`EventService` communicates with `DirectoryService` when creating or editing events and event-speaker assignments.

This communication is necessary because locations and speakers are owned by `DirectoryService`, while events only reference them by identifier.

## Asynchronous Messaging

The application also implements asynchronous communication between services using RabbitMQ.

Messaging is used for propagating changes between services after the main business operation has already been saved.

Implemented messaging flows:

* `EventService -> DirectoryService`
  * `EventCreatedEvent`
  * `EventDeletedEvent`
  * `EventSpeakerAddedEvent`
  * `EventSpeakerRemovedEvent`

* `RegistrationService -> EventService`
  * `RegistrationCreatedEvent`
  * `RegistrationDeletedEvent`

## Message Channels

The application uses named RabbitMQ exchanges, queues and routing keys as logical message channels.

### EventService -> DirectoryService channel

This channel is used for event-related changes that affect the usage of locations and speakers.

* Exchange: `smart-event.event-integration`
* Routing key: `event.directory-usage.changed`
* Queue: `directory.event-usage.queue`

`DirectoryService` consumes these messages and updates local tracker tables:

* `LocationUsageTrackers`
* `SpeakerUsageTrackers`

This allows `DirectoryService` to check whether a location or speaker is used without making a direct HTTP call to `EventService`.

### RegistrationService -> EventService channel

This channel is used for registration-related changes that affect the number of registrations for an event.

* Exchange: `smart-event.registration-integration`
* Routing key: `registration.event-usage.changed`
* Queue: `event.registration-usage.queue`

`EventService` consumes these messages and updates the local tracker table:

* `EventRegistrationTrackers`

This allows `EventService` to know whether an event has registrations without depending only on direct synchronous calls.

## Message Types

The application uses integration events as message types.

Integration events represent something that has already happened in one service.

Examples:

* `EventCreatedEvent`
* `EventDeletedEvent`
* `EventSpeakerAddedEvent`
* `EventSpeakerRemovedEvent`
* `RegistrationCreatedEvent`
* `RegistrationDeletedEvent`

These messages are events, not commands. The producer does not tell the consumer what method to execute. It only publishes the fact that something happened, and the consumer decides how to react.

## Outbox Pattern

Producer services do not publish RabbitMQ messages directly from controller logic only.

Instead, they first store integration events in a local `OutboxMessages` table in the same database transaction as the business change.

A background service later reads pending outbox messages and publishes them to RabbitMQ.

This prevents message loss if the database operation succeeds but RabbitMQ is temporarily unavailable.

## Idempotent Consumers

Consumer services store processed message identifiers in a `ProcessedMessages` table.

This makes consumers idempotent, because receiving the same RabbitMQ message multiple times does not duplicate the business effect.

## Resiliency Mechanisms

The application implements the following resiliency mechanisms for synchronous inter-service HTTP communication:

* retry
* timeout
* circuit breaker

These mechanisms are implemented to make communication between services more reliable and to prevent the whole system from failing immediately when one service is temporarily unavailable.

## Retry

Retry is implemented using Polly.

If a temporary communication failure occurs, the request is repeated before the operation is considered failed.

This is useful for short temporary failures such as:

* network interruptions
* temporary service unavailability
* transient HTTP failures

## Timeout

Timeout is configured through `HttpClient.Timeout`.

This prevents one service from waiting indefinitely for another service to respond.

If the called service does not respond within the configured time, the request is cancelled and handled as a timeout error.

## Circuit Breaker

Circuit breaker is implemented manually using a custom circuit breaker class, following the approach used in the exercise example.

If repeated failures occur while calling another service, the circuit breaker opens and temporarily prevents further calls to that service.

This prevents unnecessary repeated calls to a service that is currently unavailable.

The circuit breaker supports the following states:

* `Closed`
* `Open`
* `HalfOpen`

## Error Handling

Backend services use global exception handling to convert exceptions into appropriate HTTP responses.

Examples:

* business validation errors are returned as client errors
* timeout errors are returned as gateway timeout responses
* circuit breaker errors are returned as service unavailable responses
* unexpected errors are returned as internal server errors

The frontend handles these responses and displays user-friendly error messages instead of stopping the application.

## Demonstrated Communication Scenarios

The resiliency mechanisms are demonstrated on service-to-service HTTP communication flows such as:

* `RegistrationService -> EventService`
* `EventService -> RegistrationService`
* `EventService -> DirectoryService`

Example scenarios:

* checking event information before creating or editing a registration
* checking event capacity before allowing a registration
* checking whether an event has registrations before deleting it
* checking whether a location exists before creating or editing an event
* checking whether a speaker exists before assigning the speaker to an event

Asynchronous messaging is demonstrated through RabbitMQ flows:

* `EventService -> DirectoryService`
* `RegistrationService -> EventService`

The application also implements asynchronous communication between services using RabbitMQ.

Implemented messaging flows:

* `RegistrationService -> EventService`
  * `RegistrationCreatedEvent`
  * `RegistrationDeletedEvent`

* `EventService -> DirectoryService`
  * `EventCreatedEvent`
  * `EventDeletedEvent`
  * `EventSpeakerAddedEvent`
  * `EventSpeakerRemovedEvent`

The producer services do not publish messages directly from controller logic only.
Instead, they first store integration events in a local `OutboxMessages` table in the same database transaction as the business change.

A background service later reads pending outbox messages and publishes them to RabbitMQ.

Consumer services store processed message identifiers in a `ProcessedMessages` table.
This makes consumers idempotent, because receiving the same RabbitMQ message multiple times does not duplicate the business effect.

This follows the outbox pattern and prevents message loss if the database operation succeeds but RabbitMQ is temporarily unavailable.

## Technology Stack

* ASP.NET Core MVC
* ASP.NET Core Web API
* .NET 9
* Entity Framework Core
* SQL Server
* Razor Views
* HttpClientFactory
* Polly
* RabbitMQ

## Notes

The current version follows a microservice-based architecture with:

* separate backend services
* separate service responsibilities
* separate databases
* shared DTO contracts
* HTTP-based service communication
* retry mechanism
* timeout mechanism
* manual circuit breaker mechanism
* global exception handling
* frontend error handling

The decomposition separates event organization, directory management, and participant registration into different services, making the system more modular and aligned with microservice architecture principles.
