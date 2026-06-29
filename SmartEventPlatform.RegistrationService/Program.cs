using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.RegistrationService.Clients;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.ErrorHandling;
using SmartEventPlatform.RegistrationService.Messaging;
using SmartEventPlatform.RegistrationService.Resilience;
using SmartEventPlatform.RegistrationService.Saga;

namespace SmartEventPlatform.RegistrationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<RegistrationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddSingleton<EventServiceCircuitBreaker>();

            builder.Services.AddHttpClient<IEventServiceClient, EventServiceClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:EventService"]!);
                client.Timeout = TimeSpan.FromSeconds(3);
            });

            // Saga: HTTP klijent prema DirectoryService (Korak 3 Sage)
            builder.Services.AddHttpClient<IDirectoryServiceClient, DirectoryServiceClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:DirectoryService"]!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            // Saga: Orkestrator (Scoped jer koristi DbContext koji je Scoped)
            builder.Services.AddScoped<RegistrationSagaOrchestrator>();

            // Outbox za registration events prema EventService-u
            builder.Services.Configure<RabbitMqOptions>(
                builder.Configuration.GetSection(RabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
            builder.Services.AddHostedService<OutboxMessagePublisher>();

            // Request-Reply klijent (Zadatak 4)
            builder.Services.Configure<EventQueryRabbitMqOptions>(
                builder.Configuration.GetSection(EventQueryRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IRabbitMqEventQueryClient, RabbitMqEventQueryClient>();

            // Email queue (Zadatak 4)
            builder.Services.Configure<EmailRabbitMqOptions>(
                builder.Configuration.GetSection(EmailRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IEmailQueuePublisher, EmailQueuePublisher>();
            builder.Services.AddHostedService<EmailWorkerService>();

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