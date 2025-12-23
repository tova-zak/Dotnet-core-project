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
            //await Task.Delay(1000);
            await next(httpContext);
            await httpContext.Response.WriteAsync("our 1st nice middleware end!\n");        
        }
    }

    public static partial class MiddlewareExtensions
    {
        /*public static bool IsBetween(this int numberToCheck, int from, int to)
        {
           return numberToCheck > from 
            && numberToCheck < to; 
        }*/
        public static IApplicationBuilder UseMy1stMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<My1stMiddleware>();
        }
    }

