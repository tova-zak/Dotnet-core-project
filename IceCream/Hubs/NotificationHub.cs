using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;

namespace IceCream.Hubs
{
    public class NotificationHub : Hub
    {
        // map userId -> list of connection ids
        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> connections = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("userId")?.Value ?? "anonymous";
            var bag = connections.GetOrAdd(userId, _ => new ConcurrentBag<string>());
            bag.Add(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value ?? "anonymous";
            if (connections.TryGetValue(userId, out var bag))
            {
                // ConcurrentBag does not support removal; entries will be ignored when sending (SendAsync uses current ids)
            }
            return base.OnDisconnectedAsync(exception);
        }

        // server can call this to notify a specific user by id
        public static Task NotifyUser(IHubContext<NotificationHub> hubContext, string userId, string message)
        {
            if (connections.TryGetValue(userId, out var bag))
            {
                var ids = bag.ToArray();
                return hubContext.Clients.Clients(ids).SendAsync("Notify", message);
            }
            return Task.CompletedTask;
        }
    }
}
