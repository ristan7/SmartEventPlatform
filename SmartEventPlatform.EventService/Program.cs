
using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.EventService.Data;
using SmartEventPlatform.EventService.Services;

namespace SmartEventPlatform.EventService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<EventDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHttpClient<IRegistrationServiceClient, RegistrationServiceClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["RegistrationServiceEndpoint"]!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddControllers();
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
