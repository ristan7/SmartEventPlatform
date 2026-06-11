
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.RegistrationService.Clients;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.ErrorHandling;
using SmartEventPlatform.RegistrationService.Resilience;

namespace SmartEventPlatform.RegistrationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

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

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
