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
            Console.WriteLine($"[NotificationHub] OnConnected: user={userId}, connectionId={Context.ConnectionId}, totalConnectionsForUser={userConnections.Count}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value ?? "anonymous";
            if (connections.TryGetValue(userId, out var userConnections))
            {
                userConnections.TryRemove(Context.ConnectionId, out _);
                Console.WriteLine($"[NotificationHub] OnDisconnected: user={userId}, connectionId={Context.ConnectionId}, remainingConnectionsForUser={userConnections.Count}");
                if (userConnections.IsEmpty)
                {
                    connections.TryRemove(userId, out _);
                    Console.WriteLine($"[NotificationHub] Removed user entry: {userId}");
                }
            }
            else
            {
                Console.WriteLine($"[NotificationHub] OnDisconnected: user={userId} had no connections registered (connectionId={Context.ConnectionId})");
            }
            return base.OnDisconnectedAsync(exception);
        }

        // server can call this to notify a specific user by id
        public static Task NotifyUser(IHubContext<NotificationHub> hubContext, string userId, string message)
        {
            Console.WriteLine($"[NotificationHub] NotifyUser called for user={userId} message={message}");
            if (connections.TryGetValue(userId, out var userConnections))
            {
                var ids = userConnections.Keys.ToArray();
                Console.WriteLine($"[NotificationHub] Sending to {ids.Length} connection(s): {string.Join(',', ids)}");
                if (ids.Length > 0)
                {
                    return hubContext.Clients.Clients(ids).SendAsync("Notify", message);
                }
            }
            else
            {
                Console.WriteLine($"[NotificationHub] No connections found for user={userId}");
            }
            return Task.CompletedTask;
        }
    }
}
