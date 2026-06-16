using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.DirectoryService.Clients;
using SmartEventPlatform.DirectoryService.Data;
using SmartEventPlatform.DirectoryService.ErrorHandling;
using SmartEventPlatform.DirectoryService.Messaging;
using SmartEventPlatform.DirectoryService.Resilience;

namespace SmartEventPlatform.DirectoryService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<DirectoryDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.Configure<RabbitMqOptions>(
                builder.Configuration.GetSection(RabbitMqOptions.SectionName));
            builder.Services.AddHostedService<EventEventsConsumerService>();

            builder.Services.AddSingleton<EventServiceCircuitBreaker>();

            //builder.Services.AddHttpClient<IEventUsageClient, EventUsageClient>(client =>
            //{
            //    client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:EventService"]!);
            //    client.Timeout = TimeSpan.FromSeconds(3);
            //});

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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
