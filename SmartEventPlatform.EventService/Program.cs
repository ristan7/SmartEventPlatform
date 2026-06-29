using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Clients;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.ErrorHandling;
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

            builder.Services.Configure<PublisherRabbitMqOptions>(
                builder.Configuration.GetSection(PublisherRabbitMqOptions.SectionName));
            builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
            builder.Services.AddHostedService<OutboxMessagePublisher>();

            builder.Services.Configure<ConsumerRabbitMqOptions>(
                builder.Configuration.GetSection(ConsumerRabbitMqOptions.SectionName));
            builder.Services.AddHostedService<RegistrationEventsConsumerService>();

            // Zadatak 4 — Request-Reply server strana
            builder.Services.Configure<EventQueryRabbitMqOptions>(
                builder.Configuration.GetSection(EventQueryRabbitMqOptions.SectionName));
            builder.Services.AddHostedService<EventQueryConsumerService>();

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