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

            builder.Services.AddHttpClient<IDirectoryServiceClient, DirectoryServiceClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:DirectoryService"]!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            builder.Services.AddScoped<RegistrationSagaOrchestrator>();

            builder.Services.Configure<RabbitMqOptions>(
                builder.Configuration.GetSection(RabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
            builder.Services.AddHostedService<OutboxMessagePublisher>();

            builder.Services.Configure<EventQueryRabbitMqOptions>(
                builder.Configuration.GetSection(EventQueryRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IRabbitMqEventQueryClient, RabbitMqEventQueryClient>();

            //mejlovi
            builder.Services.Configure<EmailRabbitMqOptions>(
                builder.Configuration.GetSection(EmailRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IEmailQueuePublisher, EmailQueuePublisher>();
            builder.Services.AddHostedService<EmailWorkerService>();

            // ── Saga Koreografija ─────────────────────────────────────────
            builder.Services.Configure<SagaChoreographyRabbitMqOptions>(
                builder.Configuration.GetSection(SagaChoreographyRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<ISagaChoreographyPublisher, SagaChoreographyPublisher>();
            builder.Services.AddHostedService<SagaChoreographyConsumerService>();
            // ─────────────────────────────────────────────────────────────

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