# Smart Event Platform

Smart Event Platform je mikroservisna aplikacija za upravljanje stručnim događajima, lokacijama, predavačima, učesnicima i prijavama učesnika.

Aplikacija je prvobitno razvijena kao ASP.NET Core MVC rešenje, a zatim je razložena na više nezavisnih servisa. Trenutna verzija koristi mikroservisnu arhitekturu, zasebne baze po servisu, HTTP komunikaciju, RabbitMQ poruke, API Gateway, CQRS, Event Sourcing i Saga pattern.

---

## Sadržaj

- [Arhitektura sistema](#arhitektura-sistema)
- [Projekti u rešenju](#projekti-u-rešenju)
- [Dekompozicija servisa](#dekompozicija-servisa)
- [Tehnologije](#tehnologije)
- [Pokretanje aplikacije](#pokretanje-aplikacije)
- [Baze podataka i migracije](#baze-podataka-i-migracije)
- [API Gateway](#api-gateway)
- [Komunikacija između servisa](#komunikacija-između-servisa)
- [RabbitMQ messaging](#rabbitmq-messaging)
- [CQRS](#cqrs)
- [Event Sourcing](#event-sourcing)
- [Saga orkestracija](#saga-orkestracija)
- [Saga koreografija](#saga-koreografija)
- [Otpornost sistema](#otpornost-sistema)
- [Error handling](#error-handling)
- [Najbitniji endpoint-i](#najbitniji-endpoint-i)

---

## Arhitektura sistema

Sistem je podeljen na više servisa prema poslovnim odgovornostima:

```text
SmartEventPlatformWeb
        |
        v
SmartEventPlatform.ApiGateway
        |
        +------------------------------+
        |                              |
        v                              v
SmartEventPlatform.EventService   SmartEventPlatform.DirectoryService
        |
        v
SmartEventPlatform.RegistrationService

RabbitMQ se koristi za asinhronu komunikaciju između servisa.
Svaki backend servis ima svoju SQL Server bazu.
```

Frontend ne pristupa bazama direktno. On šalje zahteve ka `ApiGateway` projektu, a gateway prosleđuje zahteve odgovarajućem backend servisu.

---

## Projekti u rešenju

| Projekat | Uloga |
|---|---|
| `SmartEventPlatformWeb` | ASP.NET Core MVC frontend aplikacija sa Razor view-ovima. |
| `SmartEventPlatform.ApiGateway` | Ocelot API Gateway. Centralna ulazna tačka za frontend. |
| `SmartEventPlatform.EventService` | Servis za događaje, tipove događaja, predavače na događaju, CQRS i Event Sourcing. |
| `SmartEventPlatform.DirectoryService` | Servis za lokacije i predavače. |
| `SmartEventPlatform.RegistrationService` | Servis za učesnike, prijave, Saga orkestraciju, Saga koreografiju, request-reply i email queue. |
| `SmartEventPlatform.Contracts` | Zajednički DTO i integration event modeli. |

---

## Dekompozicija servisa

### EventService

`EventService` upravlja domenom organizacije događaja.

Odgovoran je za:

- događaje,
- tipove događaja,
- povezivanje događaja i predavača,
- snapshot podatke o lokaciji na događaju,
- snapshot podatke o predavaču na događaju,
- evidenciju broja prijava po događaju kroz tracker tabelu,
- CQRS implementaciju za događaje,
- Event Sourcing implementaciju za event-sourced događaje,
- rezervaciju mesta u okviru Saga procesa.

`EventService` ne poseduje lokacije, predavače, učesnike ni prijave. Za njih čuva samo identifikatore i snapshot podatke koji su potrebni za prikaz.

### DirectoryService

`DirectoryService` upravlja katalogom lokacija i predavača.

Odgovoran je za:

- lokacije,
- predavače,
- praćenje da li je lokacija iskorišćena u nekom događaju,
- praćenje da li je predavač iskorišćen u nekom događaju,
- praćenje broja prisustava po lokaciji u Saga procesu.

Lokacije i predavači su izdvojeni iz `EventService` zato što imaju svoj životni ciklus i mogu postojati nezavisno od konkretnog događaja.

### RegistrationService

`RegistrationService` upravlja domenom prijava učesnika.

Odgovoran je za:

- učesnike,
- prijave učesnika na događaje,
- prikaz dostupnih događaja,
- proveru događaja pre prijave,
- Saga orkestraciju,
- Saga koreografiju,
- request-reply komunikaciju sa `EventService`,
- slanje email notifikacija preko RabbitMQ reda,
- outbox pattern za događaje vezane za prijave.

Prijava se nalazi u posebnom servisu zato što predstavlja odvojen poslovni proces od organizacije događaja.

### SmartEventPlatformWeb

`SmartEventPlatformWeb` je MVC frontend.

Odgovoran je za:

- Razor view stranice,
- korisničku interakciju,
- pozive ka `ApiGateway`,
- prikaz validacionih i poslovnih grešaka,
- rad sa `ApiHttpHelper` i `ApiOperationResult` klasama.

Frontend koristi jedan named `HttpClient`: `ApiGateway`.

### Contracts

`SmartEventPlatform.Contracts` sadrži deljene modele:

- DTO klase za HTTP komunikaciju,
- integration event klase za RabbitMQ poruke,
- request-reply modele,
- Saga choreography event modele.

---

## Tehnologije

- .NET 9
- ASP.NET Core MVC
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server LocalDB
- RabbitMQ
- Ocelot API Gateway
- Polly retry
- HttpClientFactory
- Razor Views
- Swagger / OpenAPI

---

## Pokretanje aplikacije

### Preduslovi

Potrebno je imati instalirano:

- .NET 9 SDK
- SQL Server LocalDB ili drugi SQL Server
- RabbitMQ server
- Visual Studio 2022 ili noviji

Ako RabbitMQ nije instaliran lokalno, može se pokrenuti kroz Docker:

```bash
docker run -d --hostname smart-event-rabbit --name smart-event-rabbit \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management
```

RabbitMQ Management UI je tada dostupan na:

```text
http://localhost:15672
```

Podrazumevani kredencijali su:

```text
username: guest
password: guest
```

### Portovi

| Projekat | HTTP | HTTPS |
|---|---:|---:|
| `SmartEventPlatformWeb` | `http://localhost:5287` | `https://localhost:7001` |
| `SmartEventPlatform.ApiGateway` | `http://localhost:5200` | `https://localhost:7200` |
| `SmartEventPlatform.EventService` | `http://localhost:5022` | `https://localhost:7007` |
| `SmartEventPlatform.DirectoryService` | `http://localhost:5103` | `https://localhost:7103` |
| `SmartEventPlatform.RegistrationService` | `http://localhost:5001` | `https://localhost:7292` |

Frontend koristi API Gateway na:

```text
http://localhost:5200
```

Servisi u `appsettings.json` koriste HTTPS adrese za međusobne HTTP pozive, pa je najbolje pokretati backend servise sa `https` launch profilom.

### Pokretanje iz Visual Studio-a

Otvoriti:

```text
SmartEventPlatformApp.slnx
```

Pokrenuti sledeće projekte zajedno:

- `SmartEventPlatform.DirectoryService`
- `SmartEventPlatform.EventService`
- `SmartEventPlatform.RegistrationService`
- `SmartEventPlatform.ApiGateway`
- `SmartEventPlatformWeb`

Preporučeni redosled pokretanja je:

1. RabbitMQ
2. backend servisi
3. API Gateway
4. MVC frontend

### Pokretanje iz terminala

U više terminala pokrenuti:

```bash
dotnet run --project SmartEventPlatform.DirectoryService --launch-profile https
```

```bash
dotnet run --project SmartEventPlatform.EventService --launch-profile https
```

```bash
dotnet run --project SmartEventPlatform.RegistrationService --launch-profile https
```

```bash
dotnet run --project SmartEventPlatform.ApiGateway --launch-profile http
```

```bash
dotnet run --project SmartEventPlatformWeb --launch-profile https
```

---

## Baze podataka i migracije

Aplikacija koristi database-per-service pristup.

| Servis | Baza |
|---|---|
| `EventService` | `SmartEventPlatform_EventDB` |
| `DirectoryService` | `SmartEventPlatform_DirectoryDB` |
| `RegistrationService` | `SmartEventPlatform_RegistrationDB` |

Connection string-ovi se nalaze u `appsettings.json` fajlovima svakog servisa.

Migracije se primenjuju posebno za svaki servis:

```bash
dotnet ef database update \
  --project SmartEventPlatform.EventService \
  --startup-project SmartEventPlatform.EventService
```

```bash
dotnet ef database update \
  --project SmartEventPlatform.DirectoryService \
  --startup-project SmartEventPlatform.DirectoryService
```

```bash
dotnet ef database update \
  --project SmartEventPlatform.RegistrationService \
  --startup-project SmartEventPlatform.RegistrationService
```

Ako `dotnet ef` nije instaliran:

```bash
dotnet tool install --global dotnet-ef
```

---

## API Gateway

API Gateway je implementiran pomoću Ocelot biblioteke u projektu:

```text
SmartEventPlatform.ApiGateway
```

Konfiguracija se nalazi u fajlu:

```text
SmartEventPlatform.ApiGateway/ocelot.json
```

Gateway je centralna tačka kroz koju frontend pristupa servisima.

Implementirane funkcionalnosti API Gateway-a:

| Funkcionalnost | Implementacija |
|---|---|
| Rutiranje zahteva | `/gateway/...` rute se prosleđuju na odgovarajući backend servis. |
| Rate limiting | Globalno ograničenje preko `X-ClientId` header-a. |
| Keširanje | Ocelot cache za liste događaja, lokacija, predavača i tipova događaja. |
| Load balancing | Konfigurisan `LeastConnection` za pojedine rute. |
| API kompozicija | `/gateway/dashboard` agregira podatke iz više ruta. |
| API versioning | `/api/v1/events`, `/api/v1/locations`, `/api/v1/registrations`. |
| Logging & monitoring | Gateway loguje svaki request i response sa status kodom i vremenom izvršavanja. |

Primeri gateway ruta:

```text
GET    /gateway/events            -> EventService /api/events
GET    /gateway/locations         -> DirectoryService /api/locations
GET    /gateway/speakers          -> DirectoryService /api/speakers
GET    /gateway/registrations     -> RegistrationService /api/registrations
GET    /gateway/participants      -> RegistrationService /api/participants
GET    /gateway/dashboard         -> agregacija events + locations
```

Frontend automatski šalje header:

```text
X-ClientId: web-frontend
```

---

## Komunikacija između servisa

Sistem koristi dva tipa komunikacije:

1. sinhronu HTTP komunikaciju,
2. asinhronu RabbitMQ komunikaciju.

### Sinhrona HTTP komunikacija

HTTP se koristi kada servis mora odmah da dobije odgovor da bi nastavio trenutnu poslovnu operaciju.

Glavni HTTP tokovi:

```text
SmartEventPlatformWeb -> ApiGateway -> backend servisi
RegistrationService   -> EventService
RegistrationService   -> DirectoryService
EventService          -> DirectoryService
EventService          -> RegistrationService
```

Primeri:

- `RegistrationService` proverava da li događaj postoji pre kreiranja prijave.
- `RegistrationService` proverava podatke događaja i lokacije za Saga proces.
- `EventService` proverava lokaciju u `DirectoryService` pre kreiranja događaja.
- `EventService` proverava predavača u `DirectoryService` pre dodele predavača događaju.
- `EventService` proverava da li događaj ima prijave pre brisanja.

### Asinhrona komunikacija

RabbitMQ se koristi kada servis treba da obavesti drugi servis da se nešto već desilo, bez blokiranja trenutne operacije.

Primeri:

- Kada se kreira događaj, `EventService` objavljuje događaj koji `DirectoryService` koristi da evidentira korišćenje lokacije.
- Kada se obriše događaj, `EventService` objavljuje događaj koji `DirectoryService` koristi da ukloni korišćenje lokacije.
- Kada se doda predavač na događaj, `EventService` objavljuje događaj koji `DirectoryService` koristi da evidentira korišćenje predavača.
- Kada se kreira ili obriše prijava, `RegistrationService` objavljuje događaj koji `EventService` koristi za ažuriranje broja prijava.

---

## RabbitMQ messaging

### Integration event poruke

Poruke se nalaze u projektu:

```text
SmartEventPlatform.Contracts/Integration
```

Primeri integration event poruka:

- `EventCreatedEvent`
- `EventDeletedEvent`
- `EventSpeakerAddedEvent`
- `EventSpeakerRemovedEvent`
- `RegistrationCreatedEvent`
- `RegistrationDeletedEvent`
- `EventInfoRequest`
- `EventInfoReply`
- Saga choreography eventi

### EventService -> DirectoryService

`EventService` objavljuje događaje u exchange:

```text
smart-event.event-integration
```

Koriste se dve odvojene vrste poruka:

| Namena | Routing key | Queue | DLQ |
|---|---|---|---|
| Korišćenje lokacije | `event.location-usage.changed` | `directory.location-usage.queue` | `directory.location-usage.dlq` |
| Korišćenje predavača | `event.speaker-usage.changed` | `directory.speaker-usage.queue` | `directory.speaker-usage.dlq` |

`DirectoryService` obrađuje ove poruke kroz:

- `LocationUsageConsumerService`
- `SpeakerUsageConsumerService`

Lokalne tracker tabele:

- `LocationUsageTrackers`
- `SpeakerUsageTrackers`

### RegistrationService -> EventService

`RegistrationService` objavljuje događaje u exchange:

```text
smart-event.registration-integration
```

| Namena | Routing key | Queue | DLQ |
|---|---|---|---|
| Promene prijava | `registration.event-usage.changed` | `event.registration-usage.queue` | `event.registration-usage.dlq` |

`EventService` obrađuje ove poruke kroz:

```text
RegistrationEventsConsumerService
```

Lokalna tracker tabela:

```text
EventRegistrationTrackers
```

### Outbox pattern

Outbox pattern je implementiran u:

- `SmartEventPlatform.EventService`
- `SmartEventPlatform.RegistrationService`

Servis prvo snima poslovnu promenu i poruku u svoju bazu u okviru iste lokalne transakcije.

Zatim background worker čita `OutboxMessages` tabelu i objavljuje poruke na RabbitMQ.

Klase:

```text
SmartEventPlatform.EventService/Messaging/OutboxMessagePublisher.cs
SmartEventPlatform.RegistrationService/Messaging/OutboxMessagePublisher.cs
```

Time se smanjuje rizik da se desi situacija:

```text
baza uspešno snimljena, ali RabbitMQ poruka nije poslata
```

### Idempotent consumers

Consumer servisi koriste `ProcessedMessages` tabelu.

Ako ista poruka stigne više puta, servis proverava `MessageId` i ne izvršava isti poslovni efekat dva puta.

Ovo je važno zato što RabbitMQ može isporučiti istu poruku ponovo ako prethodna obrada nije potvrđena `ack` signalom.

### Dead Letter Queue

DLQ je implementiran za poruke koje ne mogu da se obrade ni nakon definisanog broja pokušaja.

Konfiguracija koristi:

```text
MaxRetryCount: 10
DeadLetterExchange: smart-event.dlx
```

Kada poruka pređe maksimalan broj pokušaja, consumer radi `BasicNack` sa `requeue: false`, pa RabbitMQ preusmerava poruku u odgovarajući DLQ.

Primeri DLQ redova:

```text
directory.location-usage.dlq
directory.speaker-usage.dlq
event.registration-usage.dlq
saga-choreo.event-service.dlq
saga-choreo.directory-service.dlq
saga-choreo.registration-service.dlq
```

### Request-Reply pattern

Request-reply je implementiran između:

```text
RegistrationService -> EventService
```

Koristi se kada `RegistrationService` želi podatke o događaju preko RabbitMQ-a i očekuje odgovor.

Konfiguracija:

```text
RequestQueue: event.query.request.queue
ReplyQueue:   event.query.reply.queue
Timeout:      5 sekundi
```

Klase:

```text
SmartEventPlatform.RegistrationService/Messaging/RabbitMqEventQueryClient.cs
SmartEventPlatform.EventService/Messaging/EventQueryConsumerService.cs
```

Tok:

1. `RegistrationService` kreira `EventInfoRequest`.
2. Postavlja `CorrelationId` i `ReplyTo`.
3. Šalje poruku u `event.query.request.queue`.
4. `EventService` čita zahtev i vraća `EventInfoReply` na reply queue.
5. `RegistrationService` povezuje odgovor sa originalnim zahtevom preko `CorrelationId`.
6. Ako odgovor ne stigne za 5 sekundi, koristi se HTTP fallback.

Request-reply se koristi u prijavi učesnika i u pokretanju Saga koreografije.

### Email queue

Slanje emailova nije direktno u kontroleru.

`RegistrationService` šalje email zahtev u RabbitMQ queue:

```text
registration.email.queue
```

Consumer je:

```text
EmailWorkerService
```

Email worker:

- čita poruke iz reda,
- primenjuje rate limit,
- ne šalje više od 10 emailova u jednom minutu,
- svaki email upisuje kao `.txt` fajl u lokalni folder `outbox`.

Konfiguracija:

```text
MaxEmailsPerMinute: 10
OutboxFolder: outbox
```

---

## CQRS

CQRS je implementiran ručno u `EventService`, bez MediatR-a i bez gotovih CQRS framework-a.

Lokacija:

```text
SmartEventPlatform.EventService/CQRS
```

CQRS razdvaja:

- command operacije koje menjaju stanje,
- query operacije koje samo čitaju podatke.

### Command deo

Komande:

```text
CreateEventCommand
UpdateEventCommand
DeleteEventCommand
```

Handleri:

```text
CreateEventCommandHandler
UpdateEventCommandHandler
DeleteEventCommandHandler
```

Write repository:

```text
IEventWriteRepository
EventWriteRepository
```

Command operacije rade validaciju i menjaju stanje sistema.

Primeri validacije:

- lokacija mora postojati u `DirectoryService`,
- tip događaja mora postojati,
- događaj sa predavačima ne može biti obrisan,
- događaj sa prijavama ne može biti obrisan.

Command handleri mogu kreirati i outbox poruke, ali ne vraćaju složene read modele.

### Query deo

Query handleri:

```text
GetAllEventsQueryHandler
GetEventByIdQueryHandler
GetUpcomingEventsQueryHandler
```

Read repository:

```text
IEventReadRepository
EventReadRepository
```

Read model:

```text
EventReadModel
```

Query operacije samo čitaju podatke i projektuju ih u modele za prikaz.

Primeri query operacija:

- lista svih događaja,
- detalji jednog događaja,
- budući događaji.

---

## Event Sourcing

Event Sourcing je implementiran u `EventService` za poseban event-sourced model događaja.

Lokacija:

```text
SmartEventPlatform.EventService/EventSourcing
SmartEventPlatform.EventService/Controllers/EventSourcedController.cs
```

Ideja je da se stanje događaja ne menja direktno, već se svaka promena čuva kao domenski događaj.

Glavni elementi:

| Element | Uloga |
|---|---|
| `EventAggregate` | Agregat koji predstavlja trenutno stanje rekonstruisano iz događaja. |
| `EventDomainEvent` | Bazna klasa za domenske događaje. |
| `EventStoreEntry` | Red u bazi koji čuva jedan događaj. |
| `EventSnapshotEntry` | Red u bazi koji čuva snapshot stanja. |
| `EventStoreRepository` | Čuva događaje, učitava istoriju i koristi snapshot. |

Domenski događaji:

```text
EventCreatedDomainEvent
EventRenamedDomainEvent
EventRescheduledDomainEvent
EventFeeChangedDomainEvent
EventLocationChangedDomainEvent
EventCancelledDomainEvent
```

Event Sourcing endpoint-i:

```text
POST   /api/eventsourced
GET    /api/eventsourced/{id}
GET    /api/eventsourced/{id}/history
PUT    /api/eventsourced/{id}/rename
PUT    /api/eventsourced/{id}/reschedule
PUT    /api/eventsourced/{id}/fee
PUT    /api/eventsourced/{id}/location
POST   /api/eventsourced/{id}/cancel
POST   /api/eventsourced/{id}/snapshot
```

Princip rada:

1. Poslovna metoda proverava pravila.
2. Ako je validno, kreira se domenski događaj.
3. `RaiseEvent` primenjuje događaj na agregat.
4. Događaj se čuva u `EventStoreEntries` tabeli.
5. Trenutno stanje se kasnije dobija rekonstrukcijom iz istorije događaja.
6. Snapshot skraćuje rekonstrukciju tako što se polazi od poslednjeg snimljenog stanja.

---

## Saga orkestracija

Saga orkestracija je implementirana u `RegistrationService`.

Glavna klasa:

```text
SmartEventPlatform.RegistrationService/Saga/RegistrationSagaOrchestrator.cs
```

Saga se pokreće pri kreiranju prijave:

```text
POST /api/registrations
```

Poslovni scenario:

```text
Učesnik se prijavljuje na stručni događaj.
Potrebno je kreirati prijavu, rezervisati mesto, evidentirati prisustvo na lokaciji i poslati email.
```

Koraci:

1. `RegistrationService` kreira `Pending` prijavu.
2. `EventService` rezerviše mesto za događaj.
3. `DirectoryService` evidentira prisustvo na lokaciji.
4. `RegistrationService` potvrđuje prijavu kao `Confirmed`.
5. `RegistrationService` stavlja email notifikaciju u queue.
6. `EventService` potvrđuje rezervaciju.

Stanje Sage se čuva u tabeli:

```text
SagaStates
```

Mogući statusi su, na primer:

```text
Started
RegistrationCreated
SpotReserved
AttendanceRecorded
Completed
Compensating
Compensated
Failed
```

Kompenzacije:

| Korak koji se poništava | Kompenzaciona akcija |
|---|---|
| Kreirana pending prijava | Brisanje pending prijave. |
| Rezervisano mesto | Oslobađanje mesta u `EventService`. |
| Evidentirano prisustvo | Uklanjanje prisustva u `DirectoryService`. |

Saga orkestracija je sinhrona iz ugla korisnika: korisnik čeka rezultat kreiranja prijave.

---

## Saga koreografija

Saga koreografija je implementirana kao asinhroni proces zasnovan na događajima.

Pokreće se preko endpoint-a:

```text
POST /api/saga-choreography/start
```

Status se proverava preko:

```text
GET /api/saga-choreography/{correlationId}/status
```

Za razliku od orkestracije, koreografija vraća:

```text
202 Accepted
```

odmah nakon pokretanja procesa.

Tok se prati preko:

```text
CorrelationId
```

RabbitMQ exchange:

```text
smart-event.saga-choreography
```

Učestvuju tri servisa:

- `RegistrationService`
- `EventService`
- `DirectoryService`

Svaki servis samostalno reaguje na događaje relevantne za njega i objavljuje sledeći događaj.

Primer toka:

1. `RegistrationService` kreira pending prijavu i objavljuje `SagaChoreographyStartedEvent`.
2. `EventService` rezerviše mesto i objavljuje događaj o rezervaciji.
3. `DirectoryService` evidentira prisustvo i objavljuje događaj o uspehu ili neuspehu.
4. `RegistrationService` potvrđuje prijavu ili pokreće kompenzaciju.
5. Ako nešto padne, objavljuju se kompenzacioni događaji.

Stanje koreografije se čuva u tabeli:

```text
SagaChoreographyStates
```

---

## Otpornost sistema

Sistem implementira sledeće mehanizme otpornosti:

- retry,
- timeout,
- circuit breaker,
- DLQ,
- outbox pattern,
- idempotent consumers,
- HTTP fallback kod request-reply timeout-a.

### Retry

Retry je implementiran pomoću Polly biblioteke.

Koristi se za HTTP pozive između servisa kada može doći do privremenog problema, na primer:

- servis se tek pokreće,
- mrežni problem,
- privremeni timeout,
- kratkotrajna nedostupnost downstream servisa.

### Timeout

Timeout je konfigurisan kroz `HttpClient.Timeout`.

Primeri:

```text
EventService -> DirectoryService: 3 sekunde
EventService -> RegistrationService: 3 sekunde
RegistrationService -> EventService: 3 sekunde
RegistrationService -> DirectoryService: 5 sekundi
```

Timeout sprečava da jedan servis beskonačno čeka drugi servis.

### Circuit breaker

Circuit breaker je implementiran ručno kroz custom klase.

Primeri:

```text
ManualCircuitBreaker
EventServiceCircuitBreaker
DirectoryServiceCircuitBreaker
RegistrationServiceCircuitBreaker
```

Stanja circuit breaker-a:

```text
Closed
Open
HalfOpen
```

Ako servis više puta ne uspe da pozove drugi servis, circuit breaker prelazi u `Open` stanje i privremeno blokira dalje pozive.

---

## Error handling

Backend servisi koriste globalni exception handler:

```text
GlobalExceptionHandler
```

On pretvara izuzetke u odgovarajuće HTTP odgovore.

Primeri:

| Situacija | HTTP status |
|---|---:|
| Resurs nije pronađen | `404 Not Found` |
| Poslovna greška / validacija | `400 Bad Request` |
| Circuit breaker otvoren | `503 Service Unavailable` |
| Timeout | `504 Gateway Timeout` |
| Neočekivana greška | `500 Internal Server Error` |

Frontend ima:

```text
MvcExceptionLoggingFilter
ApiHttpHelper
ApiOperationResult
```

Ove klase omogućavaju da MVC aplikacija prikaže korisniku jasnu poruku umesto tehničkog exception-a.

---

## Najbitniji endpoint-i

### Gateway endpoint-i

```text
GET    /gateway/events
GET    /gateway/events/{id}
POST   /gateway/events
PUT    /gateway/events/{id}
DELETE /gateway/events/{id}

GET    /gateway/locations
POST   /gateway/locations
PUT    /gateway/locations/{id}
DELETE /gateway/locations/{id}

GET    /gateway/speakers
POST   /gateway/speakers
PUT    /gateway/speakers/{id}
DELETE /gateway/speakers/{id}

GET    /gateway/participants
POST   /gateway/participants
PUT    /gateway/participants/{id}
DELETE /gateway/participants/{id}

GET    /gateway/registrations
POST   /gateway/registrations
PUT    /gateway/registrations/{id}
DELETE /gateway/registrations/{id}

GET    /gateway/dashboard
```

### Backend endpoint-i za servise

`EventService`:

```text
/api/events
/api/eventtypes
/api/eventspeakers
/api/eventsourced
/api/saga/events/{eventId}/reserve-spot
/api/saga/events/{eventId}/confirm-spot
/api/saga/events/{eventId}/release-spot
```

`DirectoryService`:

```text
/api/locations
/api/speakers
/api/saga/locations/{locationId}/record-attendance
/api/saga/locations/{locationId}/release-attendance
```

`RegistrationService`:

```text
/api/participants
/api/registrations
/api/availableevents
/api/saga-choreography/start
/api/saga-choreography/{correlationId}/status
```

---

## Napomena za testiranje

Za osnovno testiranje aplikacije potrebno je:

1. pokrenuti RabbitMQ,
2. primeniti migracije za sva tri backend servisa,
3. pokrenuti sva tri backend servisa,
4. pokrenuti API Gateway,
5. pokrenuti MVC frontend,
6. uneti lokacije, predavače i tipove događaja,
7. kreirati događaj,
8. dodati predavača na događaj,
9. kreirati učesnika,
10. kreirati prijavu učesnika na događaj.

Tok prijave učesnika demonstrira više važnih delova sistema:

- request-reply prema `EventService`,
- HTTP fallback ako request-reply istekne,
- Saga orkestraciju,
- rezervaciju mesta,
- evidenciju prisustva,
- potvrdu registracije,
- email queue,
- RabbitMQ poruke,
- outbox pattern,
- ažuriranje tracker tabela.
