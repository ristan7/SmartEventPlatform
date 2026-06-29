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

            // -------------------------------------------------------
            // KONFIGURACIJA: Ucitaj ocelot.json pored appsettings.json
            // -------------------------------------------------------
            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddOcelot()            // ucitava ocelot.json
                .AddEnvironmentVariables();

            // -------------------------------------------------------
            // LOGGING (Funkcionalnost 6: Logging & Monitoring)
            // Koristimo ugradeni .NET logging - svaki zahtev ce biti
            // logovan sa metodom, putanjom, statusom i trajanjem.
            // -------------------------------------------------------
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

            // -------------------------------------------------------
            // OCELOT registracija
            // AddCacheManager aktivira Funkcionalnost 3: Kesiranje
            // -------------------------------------------------------
            builder.Services
                .AddOcelot(builder.Configuration)
                .AddCacheManager(x => x.WithDictionaryHandle());

            var app = builder.Build();

            // -------------------------------------------------------
            // CUSTOM MIDDLEWARE - Logging & Monitoring
            // -------------------------------------------------------
            // Ovaj middleware se izvrsava PRIJE Ocelota i loguje:
            //   - HTTP metoda i putanja dolaznog zahteva
            //   - Status kod odgovora i trajanje u ms
            // Ovo demonstrira Funkcionalnost 6: Logging & Monitoring
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
            var pipelineConfig = new OcelotPipelineConfiguration
            {
                // Custom authorization middleware - ovdje mozemo dodati
                // logiku autentikacije/autorizacije u buducnosti
                AuthorizationMiddleware = async (context, next) =>
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    logger.LogDebug(
                        "[GATEWAY AUTH] Provjera autorizacije za {Path}",
                        context.Request.Path);

                    // Trenutno propustamo sve zahtjeve - za produkciju
                    // ovdje bi isla provjera JWT tokena ili API kljuca
                    await next.Invoke();
                }
            };

            await app.UseOcelot(pipelineConfig);

            await app.RunAsync();
        }
    }
}