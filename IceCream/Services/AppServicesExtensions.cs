using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IceCream.Services
{
    public static class AppServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services)
        {
            // existing service registrations
            services.AddSingleton<IceCream.Interfaces.IUserService, UserService>();

            // Active user for per-request scoped info
            services.AddScoped<IceCream.Interfaces.IActiveUser, ActiveUser>();

            // Log queue + background worker
            services.AddSingleton<LogQueue>();
            services.AddHostedService<LogWorker>();

            // SignalR
            services.AddSignalR();
        }
    }
}