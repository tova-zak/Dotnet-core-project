using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IceCream.Services
{
    public static class AppServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services)
        {
            services.AddSingleton<IceCream.Interfaces.IUserService, UserService>();
            services.AddScoped<IceCream.Interfaces.IActiveUser, ActiveUser>();
            services.AddSingleton<LogQueue>();
            services.AddHostedService<LogWorker>();
            services.AddSignalR();
        }
    }
}