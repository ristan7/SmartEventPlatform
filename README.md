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

The services communicate using synchronous HTTP communication.

The main communication flows are:

* `SmartEventPlatformWeb -> EventService`
* `SmartEventPlatformWeb -> DirectoryService`
* `SmartEventPlatformWeb -> RegistrationService`
* `EventService -> DirectoryService`
* `EventService -> RegistrationService`
* `RegistrationService -> EventService`

### RegistrationService -> EventService

`RegistrationService` communicates with `EventService` when creating or editing registrations.

This communication is necessary because `RegistrationService` must verify that:

* the selected event exists
* the event is available for registration
* the event location capacity has not been reached

### EventService -> RegistrationService

`EventService` communicates with `RegistrationService` before deleting an event.

This communication is necessary because an event that already has participant registrations must not be deleted.

### EventService -> DirectoryService

`EventService` communicates with `DirectoryService` when working with event locations and speakers.

This communication is necessary because locations and speakers are owned by `DirectoryService`, while events only reference them.

### DirectoryService -> EventService

`DirectoryService` communicates with `EventService` before deleting a location or speaker.

This communication is necessary because a location or speaker that is already used by an event must not be deleted.

## Resiliency Mechanisms

The application implements the following resiliency mechanisms for inter-service communication:

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

The resiliency mechanisms are demonstrated on service-to-service communication flows such as:

* `RegistrationService -> EventService`
* `EventService -> RegistrationService`
* `EventService -> DirectoryService`
* `DirectoryService -> EventService`

Example scenarios:

* checking event information before creating or editing a registration
* checking event capacity before allowing a registration
* checking whether an event has registrations before deleting it
* checking whether a location is used before deleting it
* checking whether a speaker is used before deleting it

## Asynchronous Messaging - Task 3

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
