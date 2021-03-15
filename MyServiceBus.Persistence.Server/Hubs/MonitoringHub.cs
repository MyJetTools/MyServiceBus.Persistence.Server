using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MyServiceBus.Persistence.Server.Hubs
{
    public class MonitoringHub : Hub
    {

        private static readonly MonitoringHubConnectionList ConnectionsList = new MonitoringHubConnectionList();
        
        public override async Task OnConnectedAsync()
        {
            var newConnection = new MonitoringHubConnection(Context.ConnectionId, Clients.Caller);
            ConnectionsList.Add(newConnection);
            Console.WriteLine("Monitoring Connection: "+Context.ConnectionId);
            await newConnection.SendInitAsync();
            await SyncDataWithSocketAsync(newConnection);
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine("Monitoring Connection dropped: "+Context.ConnectionId);
            ConnectionsList.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }


        private static async ValueTask SyncDataWithSocketAsync(MonitoringHubConnection connection)
        {
            await connection.SendTopicsAsync();
            await connection.SendTopicsInfo();
        }
    }
}