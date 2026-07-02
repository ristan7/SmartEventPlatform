using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace SmartEventPlatform.ApiGateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddOcelot()
                .AddEnvironmentVariables();

            //zahtev 11 logging and monitoring
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

            //zahtev 8 kesiranje odgovora
            builder.Services
                .AddOcelot(builder.Configuration)
                .AddCacheManager(x => x.WithDictionaryHandle());

            var app = builder.Build();

            
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var method = context.Request.Method;
                var path = context.Request.Path;

                logger.LogInformation(
                    "[GATEWAY REQUEST]  {Method} {Path}",
                    method, path);

                await next.Invoke();

                stopwatch.Stop();
                logger.LogInformation(
                    "[GATEWAY RESPONSE] {Method} {Path} -> {StatusCode} ({ElapsedMs}ms)",
                    method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            });

            // -------------------------------------------------------
            // OCELOT pipeline
            // Ocelot ovdje preuzima kontrolu i radi:
            //   - Rutiranje (Funk. 1)
            //   - Rate Limiting (Funk. 2)
            //   - Kesiranje (Funk. 3)
            //   - Load Balancing (Funk. 4)
            //   - API Kompozicija (Funk. 5)
            // Sve su konfigurisane u ocelot.json
            // -------------------------------------------------------
            //var pipelineConfig = new OcelotPipelineConfiguration
            //{
            //    
            //    AuthorizationMiddleware = async (context, next) =>
            //    {
            //        var logger = context.RequestServices
            //            .GetRequiredService<ILogger<Program>>();

            //        logger.LogDebug(
            //            "[GATEWAY AUTH] Provjera autorizacije za {Path}",
            //            context.Request.Path);

            //        // Trenutno propustamo sve zahtjeve - za produkciju
            //        // ovdje bi isla provjera JWT tokena ili API kljuca
            //        await next.Invoke();
            //    }
            //};

            await app.UseOcelot();

            await app.RunAsync();
        }
    }
}