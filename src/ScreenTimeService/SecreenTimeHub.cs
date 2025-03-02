using Microsoft.AspNetCore.SignalR;
using ScreenTime.Common;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using ScreenTimeService.Models;

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

        // New hub method to request an extension.
        public async Task RequestExtension(ScreenTime.Common.ExtensionRequest request)
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext.RequestServices.GetRequiredService<UserContext>();
            var user = GetOrEnsureUser(db, Context.User);
            if (user == null)
                throw new HubException("User not found");

            request.UserId = user.Id;
            request.SubmissionDate = TimeProvider.System.GetUtcNow();
            request.IsActive = true;
            db.ExtensionRequests.Add(request);
            await db.SaveChangesAsync();
            await Clients.Caller.SendAsync("ExtensionRequestResponse", $"Extension {request.Id} logged for {user.UserName}.");
        }

        // New hub method to deny extension requests.
        public async Task DenyExtension(Guid[] ids)
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext.RequestServices.GetRequiredService<UserContext>();
            var user = GetOrEnsureUser(db, Context.User);
            if (user == null)
                throw new HubException("User not found");

            var requests = db.ExtensionRequests.Where(r => ids.Contains(r.Id)).ToList();
            requests.ForEach(r => r.IsActive = false);
            db.SaveChanges();
            await Clients.Caller.SendAsync("DenyExtensionResponse", $"{user.UserName} denied {requests.Count} requests.");
        }

        // New hub method to approve extension requests.
        public async Task ApproveExtension(AdminExtensionGrant grant)
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext.RequestServices.GetRequiredService<UserContext>();
            var user = GetOrEnsureUser(db, Context.User);
            if (user == null)
                throw new HubException("User not found");

            var requests = db.ExtensionRequests
                .Where(r => grant.RequestIds.Contains(r.Id) && grant.UserId == r.UserId)
                .ToList();
            requests.ForEach(r => r.IsActive = false);

            db.ExtensionRequestResponses.Add(new ExtensionRequestResponse
            {
                GrantedByUserId = user.Id,
                GrantedForUserId = grant.UserId,
                GratedDateTime = TimeProvider.System.GetUtcNow(),
                GrantedForDate = TimeProvider.System.GetUtcNow(),
                GrantedDuration = grant.Duration,
                DismissedExtensionRequests = requests
            });
            db.SaveChanges();
            await Clients.Caller.SendAsync("ApproveExtensionResponse",
                requests.Select(r => new ExtensionGrant(TimeProvider.System.GetUtcNow(), r.Duration)));
        }

        // New hub method to retrieve active extension requests.
        public async Task GetExtensionRequests()
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext.RequestServices.GetRequiredService<UserContext>();

            var requests = db.ExtensionRequests
                .Where(r => r.IsActive)
                .OrderBy(r => r.SubmissionDate)
                .ToList();

            var users = db.Users.ToDictionary(u => u.Id);
            var adminRequests = requests.Select(r =>
                new AdminExtensionRequest(
                    r.Id,
                    new User(users[r.UserId].Id, users[r.UserId].Email, users[r.UserId].Email),
                    r.SubmissionDate,
                    r.Duration));

            await Clients.Caller.SendAsync("ExtensionRequestsResponse", adminRequests);
        }

        // New hub method for extension approvals (placeholder).
        public async Task GetExtensionApprovals()
        {
            await Clients.Caller.SendAsync("ExtensionApprovalsResponse", "Not Implemented");
        }

        // New hub method to get configuration.
        public async Task GetConfiguration()
        {
            // In a real scenario, configuration would be fetched from a store.
            var config = new DailyConfiguration(); // Minimal equivalent.
            await Clients.Caller.SendAsync("ConfigurationResponse", config);
        }

        // New hub method to send heartbeat.
        public async Task SendHeartbeat(Heartbeat heartbeat)
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext.RequestServices.GetRequiredService<UserContext>();
            var user = GetOrEnsureUser(db, Context.User);
            if (user == null)
                throw new HubException("User not found");

            var record = new HeartbeatRecord
            {
                UserId = user.Id,
                DateTime = heartbeat.Timestamp,
                Duration = heartbeat.Duration,
                UserState = heartbeat.UserState
            };

            db.Heartbeats.Add(record);
            await db.SaveChangesAsync();
            await Clients.Caller.SendAsync("HeartbeatResponse", "Heartbeat received");
        }

        // New DTO for processing extension decisions.
        public class ExtensionDecision
        {
            public Guid RequestId { get; set; }
            public TimeSpan? ApprovedDuration { get; set; } // if null then denied
        }

        // New hub method: Get grouped pending approvals by requesting user.
        public async Task GetGroupedPendingApprovals()
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext.RequestServices.GetRequiredService<UserContext>();
            // Retrieve pending requests and group them by user.
            var pending = db.ExtensionRequests
                .Where(r => r.IsActive)
                .OrderBy(r => r.SubmissionDate)
                .ToList();
            var users = db.Users.ToDictionary(u => u.Id);
            var grouped = pending.GroupBy(r => r.UserId)
                .Select(g => new {
                    User = users.ContainsKey(g.Key) ? new { users[g.Key].Id, users[g.Key].UserName, users[g.Key].Email } : null,
                    Requests = g.ToList()
                });
            await Clients.Caller.SendAsync("GroupedPendingApprovalsResponse", grouped);
        }

        // New hub method: Process extension decisions (approve with custom duration or deny)
        public async Task ProcessExtensionDecisions(List<ExtensionDecision> decisions)
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext.RequestServices.GetRequiredService<UserContext>();
            // Dictionary to group approved decisions by requesting user.
            var approvedByUser = new Dictionary<Guid, List<(Guid RequestId, TimeSpan ApprovedDuration)>>();
            
            foreach (var decision in decisions)
            {
                var requestEntity = db.ExtensionRequests.FirstOrDefault(r => r.Id == decision.RequestId && r.IsActive);
                if (requestEntity == null)
                    continue; // skip if not found or already processed

                // Mark the request as processed.
                requestEntity.IsActive = false;
                if (decision.ApprovedDuration.HasValue)
                {
                    // Group approved decisions by the request's user Id.
                    if (!approvedByUser.ContainsKey(requestEntity.UserId))
                        approvedByUser[requestEntity.UserId] = new List<(Guid, TimeSpan)>();

                    approvedByUser[requestEntity.UserId].Add((requestEntity.Id, decision.ApprovedDuration.Value));
                }
                // else: request is denied.
            }
            
            // Process approved decisions: create a response per user.
            foreach(var kvp in approvedByUser)
            {
                var userId = kvp.Key;
                var approvals = kvp.Value;
                db.ExtensionRequestResponses.Add(new ExtensionRequestResponse
                {
                    GrantedByUserId = GetOrEnsureUser(db, Context.User)?.Id ?? Guid.Empty,
                    GrantedForUserId = userId,
                    GratedDateTime = TimeProvider.System.GetUtcNow(),
                    GrantedForDate = TimeProvider.System.GetUtcNow(),
                    // For simplicity, if multiple approvals have different durations,
                    // we sum them up or pick one custom rule. Here we sum the durations.
                    GrantedDuration = approvals.Aggregate(TimeSpan.Zero, (acc, cur) => acc + cur.ApprovedDuration),
                    DismissedExtensionRequests = db.ExtensionRequests.Where(r => approvals.Select(a => a.RequestId).Contains(r.Id)).ToList()
                });
            }
            
            db.SaveChanges();
            await Clients.Caller.SendAsync("ProcessExtensionDecisionsResponse", "Processed decisions successfully.");
        }

        // Helper method, moved from Program.cs.
        private UserRecord? GetOrEnsureUser(UserContext db, ClaimsPrincipal principal)
        {
            var displayName = principal.GetDisplayName();
            var nameIdentifier = principal.GetNameIdentifierId();
            if (nameIdentifier == null || displayName == null)
            {
                throw new InvalidOperationException("Invalid user");
            }
            var user = db.Users.FirstOrDefault(u => u.NameIdentifier == nameIdentifier);
            if (user == null)
            {
                user = new UserRecord
                {
                    NameIdentifier = nameIdentifier,
                    UserName = displayName,
                    Email = displayName,
                    CreatedAt = DateTime.Now
                };
                db.Users.Add(user);
                db.SaveChanges();
            }
            return user;
        }
    }
}
