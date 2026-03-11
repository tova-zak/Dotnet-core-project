using System.Collections.Concurrent;

namespace IceCream.Services
{
    // פשוט תור לוגים שניתן להזריק לשירות שיכתוב לקובץ ברקע
    public class LogQueue
    {
        private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        public void Enqueue(string message) => queue.Enqueue(message);
        public bool TryDequeue(out string? message) => queue.TryDequeue(out message);
    }
}
