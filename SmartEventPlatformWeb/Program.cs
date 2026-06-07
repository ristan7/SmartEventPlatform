using SmartEventPlatformWeb.Services;

namespace SmartEventPlatformWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddHttpClient("EventService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:EventService"]!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            builder.Services.AddHttpClient("RegistrationService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:RegistrationService"]!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            builder.Services.AddScoped<IEventApiClient, EventApiClient>();
            builder.Services.AddScoped<IRegistrationApiClient, RegistrationApiClient>();

            builder.Services.AddControllersWithViews();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
