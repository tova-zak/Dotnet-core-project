using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using IceCream.Interfaces;

namespace IceCream.Services
{
    // Middleware that populates IActiveUser from JWT claims for the current request
    public class ActiveUserMiddleware
    {
        private readonly RequestDelegate _next;
        public ActiveUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var activeUser = context.RequestServices.GetService(typeof(IActiveUser)) as IActiveUser;
            if (activeUser != null && context.User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = context.User.FindFirst("userId");
                var roleClaim = context.User.FindFirst("type");
                var shopClaim = context.User.FindFirst("userShopName");

                if (idClaim != null && int.TryParse(idClaim.Value, out var id))
                    activeUser.Id = id;
                activeUser.Role = roleClaim?.Value;
                activeUser.ShopName = shopClaim?.Value;
            }

            await _next(context);
        }
    }

    public static class ActiveUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseActiveUser(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ActiveUserMiddleware>();
        }
    }
}
