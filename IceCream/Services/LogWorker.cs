using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.IO;
using Microsoft.Extensions.Logging;

namespace IceCream.Services
{
    // שירות ברקע שצורך הודעות תור ומייצר קובץ לוג
    public class LogWorker : BackgroundService
    {
        private readonly LogQueue queue;
        private readonly ILogger<LogWorker> logger;
        private readonly string logPath = Path.Combine("logs", "requests.log");

        public LogWorker(LogQueue queue, ILogger<LogWorker> logger)
        {
            this.queue = queue;
            this.logger = logger;
            Directory.CreateDirectory("logs");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var msg))
                {
                    try
                    {
                        await File.AppendAllTextAsync(logPath, msg + System.Environment.NewLine, stoppingToken);
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogError(ex, "Failed to write log");
                    }
                }
                else
                {
                    await Task.Delay(200, stoppingToken);
                }
            }
        }
    }
}
