using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Clients;
using SmartEventPlatform.EventService.CQRS.Commands;
using SmartEventPlatform.EventService.CQRS.Queries;
using SmartEventPlatform.EventService.CQRS.Repositories;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.ErrorHandling;
using SmartEventPlatform.EventService.EventSourcing;
using SmartEventPlatform.EventService.Messaging;
using SmartEventPlatform.EventService.Resilience;

namespace SmartEventPlatform.EventService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<EventDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            //cqrs
            builder.Services.AddScoped<IEventReadRepository, EventReadRepository>();
            builder.Services.AddScoped<IEventWriteRepository, EventWriteRepository>();

            builder.Services.AddScoped<GetAllEventsQueryHandler>();
            builder.Services.AddScoped<GetEventByIdQueryHandler>();
            builder.Services.AddScoped<GetUpcomingEventsQueryHandler>();

            builder.Services.AddScoped<CreateEventCommandHandler>();
            builder.Services.AddScoped<UpdateEventCommandHandler>();
            builder.Services.AddScoped<DeleteEventCommandHandler>();

            //event sourcing
            builder.Services.AddScoped<EventStoreRepository>();

            builder.Services.AddSingleton<DirectoryServiceCircuitBreaker>();
            builder.Services.AddSingleton<RegistrationServiceCircuitBreaker>();

            builder.Services.AddHttpClient<IDirectoryServiceClient, DirectoryServiceClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:DirectoryService"]!);
                client.Timeout = TimeSpan.FromSeconds(3);
            });

            builder.Services.AddHttpClient<IRegistrationServiceClient, RegistrationServiceClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:RegistrationService"]!);
                client.Timeout = TimeSpan.FromSeconds(3);
            });

            //outbox
            builder.Services.Configure<PublisherRabbitMqOptions>(
                builder.Configuration.GetSection(PublisherRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
            builder.Services.AddHostedService<OutboxMessagePublisher>();

            builder.Services.Configure<ConsumerRabbitMqOptions>(
                builder.Configuration.GetSection(ConsumerRabbitMqOptions.SectionName));
            builder.Services.AddHostedService<RegistrationEventsConsumerService>();

            //request-reply
            builder.Services.Configure<EventQueryRabbitMqOptions>(
                builder.Configuration.GetSection(EventQueryRabbitMqOptions.SectionName));
            builder.Services.AddHostedService<EventQueryConsumerService>();

            //koreografija
            builder.Services.Configure<SagaChoreographyRabbitMqOptions>(
                builder.Configuration.GetSection(SagaChoreographyRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<ISagaChoreographyPublisher, SagaChoreographyPublisher>();
            builder.Services.AddHostedService<SagaChoreographyConsumerService>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}