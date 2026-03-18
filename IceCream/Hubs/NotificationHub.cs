using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace IceCream.Hubs
{
    public class NotificationHub : Hub
    {
        // map userId -> set of connection ids (inner ConcurrentDictionary used as a thread-safe set)
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> connections = new();

        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("userId")?.Value ?? "anonymous";
            var userConnections = connections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            userConnections.TryAdd(Context.ConnectionId, 0);
            
            // Add admin to admin group
            var userType = Context.User?.FindFirst("type")?.Value;
            if (userType == "Admin")
            {
                return Groups.AddToGroupAsync(Context.ConnectionId, "admin").ContinueWith(_ => base.OnConnectedAsync());
            }
            
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value ?? "anonymous";
            if (connections.TryGetValue(userId, out var userConnections))
            {
                userConnections.TryRemove(Context.ConnectionId, out _);
                if (userConnections.IsEmpty)
                {
                    connections.TryRemove(userId, out _);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        // server can call this to notify a specific user by id
        public static Task NotifyUser(IHubContext<NotificationHub> hubContext, string userId, string message)
        {
            if (connections.TryGetValue(userId, out var userConnections))
            {
                var ids = userConnections.Keys.ToArray();
                if (ids.Length > 0)
                {
                    return hubContext.Clients.Clients(ids).SendAsync("Notify", message);
                }
            }
            return Task.CompletedTask;
        }

        // notify all admins
        public static Task NotifyAdmins(IHubContext<NotificationHub> hubContext, string message)
        {
            return hubContext.Clients.Group("admin").SendAsync("Notify", message);
        }
    }
}
