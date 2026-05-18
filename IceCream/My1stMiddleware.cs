namespace MyMiddleware;

    public class My1stMiddleware
    {
        private RequestDelegate next;
 
        public My1stMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            await httpContext.Response.WriteAsync("our 1st nice middleware!\n");
            await next(httpContext);
            await httpContext.Response.WriteAsync("our 1st nice middleware end!\n");        
        }
    }

    public static partial class MiddlewareExtensions
    {
        public static IApplicationBuilder UseMy1stMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<My1stMiddleware>();
        }
    }

