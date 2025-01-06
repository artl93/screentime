using Microsoft.AspNetCore.SignalR;
using ScreenTime.Common;
using System.Security.Claims;
namespace ScreenTimeService
{

    public class ScreenTimeHub : Hub
    {
        public Dictionary<string, ClaimsPrincipal> EmailToUsers { get; set; } = new Dictionary<string, ClaimsPrincipal>();

        public ClaimsPrincipal? GetUser(string email)
        {
            if (EmailToUsers.ContainsKey(email))
            {
                return EmailToUsers[email];
            }
            return null;
        }

        public async Task SendMessage(string username, UserMessage userMessage)
        {
            await Clients.User(username).SendAsync("Message", userMessage);
            // await Clients.All.SendAsync("Message", userMessage);
        }

        public async Task SendConfigurationUpdate(string username, ScreenTime.Common.DailyConfiguration configuration)
        {
            string connectionId = string.Empty;
            var configurationText = System.Text.Json.JsonSerializer.Serialize(configuration);
            await Clients.Client(connectionId).SendAsync("UpdateConfiguration", configurationText);
        }

        public override async Task OnConnectedAsync()
        {
            var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
            if (email != null && Context.User != null)
            {
                EmailToUsers[email] = Context.User;
            }
            string connectionId = Context.ConnectionId;
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            
            return base.OnDisconnectedAsync(exception);
        }
    }
}
