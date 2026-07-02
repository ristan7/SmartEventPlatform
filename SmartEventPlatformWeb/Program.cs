using SmartEventPlatformWeb.Filters;

namespace SmartEventPlatformWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            
            builder.Services.AddHttpClient("ApiGateway", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ServiceEndpoints:ApiGateway"]!);
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("X-ClientId", "web-frontend");
            });

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<MvcExceptionLoggingFilter>();
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
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