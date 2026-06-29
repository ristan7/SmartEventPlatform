using Microsoft.EntityFrameworkCore;
using SmartEventPlatform.DirectoryService.Data;
using SmartEventPlatform.DirectoryService.ErrorHandling;
using SmartEventPlatform.DirectoryService.Messaging;

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

            
            builder.Services.Configure<LocationUsageRabbitMqOptions>(
                builder.Configuration.GetSection(LocationUsageRabbitMqOptions.SectionName));
            builder.Services.AddHostedService<LocationUsageConsumerService>();

            builder.Services.Configure<SpeakerUsageRabbitMqOptions>(
                builder.Configuration.GetSection(SpeakerUsageRabbitMqOptions.SectionName));
            builder.Services.AddHostedService<SpeakerUsageConsumerService>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
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