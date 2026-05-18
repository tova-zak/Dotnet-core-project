using System.Diagnostics;

namespace MyMiddleware;

public class MyLogMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger logger;
    private readonly IceCream.Services.LogQueue logQueue;


    public MyLogMiddleware(RequestDelegate next, ILogger<MyLogMiddleware> logger, IceCream.Services.LogQueue logQueue)
    {
        this.next = next;
        this.logger = logger;
        this.logQueue = logQueue;
    }

    public async Task Invoke(HttpContext c)
    {
        var sw = new Stopwatch();
        var start = DateTime.UtcNow;
        sw.Start();
        await next.Invoke(c);
        sw.Stop();
        var duration = sw.ElapsedMilliseconds;
        var user = c.User?.FindFirst("userId")?.Value ?? "anonymous";
        var message = $"{start:O} | {c.Request.Path} {c.Request.Method} | user: {user} | durationMs: {duration}";
        logQueue.Enqueue(message);
        logger.LogInformation(message);
        
    }
}

public static partial class MiddlewareExtensions
{
    public static IApplicationBuilder UseMyLogMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MyLogMiddleware>();
    }
}

