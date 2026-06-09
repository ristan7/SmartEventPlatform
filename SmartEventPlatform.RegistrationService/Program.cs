
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.RegistrationService.Data;
using SmartEventPlatform.RegistrationService.Patterns;

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

            builder.Services.AddSingleton<CircuitBreaker>(sp =>
                new CircuitBreaker(3, TimeSpan.FromSeconds(10))
            );

            builder.Services.AddHttpClient("EventService", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                client.BaseAddress = new Uri(builder.Configuration["EventServiceEndpoint"]!);
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

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
