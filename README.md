# Event Management Platform

Platform for managing professional events, speakers, locations, participants, and participant registrations.

## Architecture

The application is implemented as a microservice-based system.

The solution is decomposed into the following projects:

- `SmartEventPlatform.EventService`
- `SmartEventPlatform.RegistrationService`
- `SmartEventPlatformWeb`
- `SmartEventPlatform.Contracts`

## Service Decomposition

### EventService

`EventService` is responsible for the event catalogue and event organization domain.

It manages:

- events
- locations
- speakers
- event types
- event-speaker assignments

These functionalities are grouped in the same service because they describe professional events and their organizational structure. Events depend on locations, event types, and speakers, so they belong to the same bounded context.

### RegistrationService

`RegistrationService` is responsible for the participant registration domain.

It manages:

- participants
- registrations
- available events calculation

Registrations are separated into their own service because they represent a different business process from event organization. This service stores only the `EventId` as a reference to an event owned by `EventService`.

### SmartEventPlatformWeb

`SmartEventPlatformWeb` is the MVC frontend application.

It does not access service databases directly. Instead, it communicates with backend services through HTTP clients.

### Contracts

`SmartEventPlatform.Contracts` contains shared DTO classes used for communication between the frontend and backend services.

This project is used to standardize request and response models between services.

## Inter-Service Communication

The services communicate using synchronous HTTP communication.

`RegistrationService` communicates with `EventService` when creating or editing registrations. This communication is necessary because `RegistrationService` must verify that the selected event exists and that the event location capacity has not been reached.

`EventService` communicates with `RegistrationService` before deleting an event. This communication is necessary because an event that already has participant registrations must not be deleted.

## Resiliency Mechanisms

The application implements the following resiliency mechanisms for inter-service communication:

- retry
- timeout
- circuit breaker

### Retry

Retry is implemented using Polly.

If a temporary communication failure occurs, the request is repeated before the operation is considered failed.

### Timeout

Timeout is configured through `HttpClient.Timeout`.

This prevents one service from waiting indefinitely for another service to respond.

### Circuit Breaker

Circuit breaker is implemented manually using a custom `CircuitBreaker` class, following the approach used in the exercise example.

If repeated failures occur while calling another service, the circuit breaker opens and temporarily prevents further calls to that service.

## Demonstrated Communication Scenarios

The resiliency mechanisms are demonstrated on the following service-to-service communication flows:

- `RegistrationService -> EventService`
- `EventService -> RegistrationService`

Examples:

- checking event information before creating or editing a registration
- loading events for available events calculation
- checking whether an event has registrations before deleting it

## Technology Stack

- ASP.NET Core MVC
- ASP.NET Core Web API
- .NET 9
- Entity Framework Core
- SQL Server
- Razor Views
- HttpClientFactory
- Polly

## Notes

The application was originally developed as a monolithic MVC application and was later decomposed into multiple services.

The current version follows a microservice-based architecture with separate backend services, separate databases, shared DTO contracts, HTTP-based communication, and resiliency mechanisms implemented according to the exercise example.