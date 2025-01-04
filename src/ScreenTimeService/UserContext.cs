using Microsoft.EntityFrameworkCore;
using ScreenTimeService;

public class UserContext : DbContext
{
    public UserContext(DbContextOptions<UserContext> options) : base(options) { }

    // public DbSet<UserEvent> UserEvents { get; set; }
    // public DbSet<UserConfiguration> UserConfigurations { get; set; }

    public DbSet<DailyConfiguration> DailyConfigurations { get; set; }
    public DbSet<WeeklyConfiguration> WeeklyConfigurations { get; set; }
    public DbSet<UserRecord> Users { get; set; }
    public DbSet<HeartbeatRecord> Heartbeats { get; set; }
    public DbSet<ScreenTimeSummary> ScreenTimeSummaries { get; set; }
    public DbSet<ExtensionRequest> ExtensionRequests { get; set; }
    public DbSet<ExtensionRequestResponse> ExtensionRequestResponses { get; set; }

}

namespace ScreenTimeService
{

    public class DailyConfiguration
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int DailyLimitMinutes { get; set; }
        public int GraceMinutes { get; set; }
        public int WarningIntervalSeconds { get; set; }
        public int WarningTimeMinutes { get; set; }
    }

    public class WeeklyConfiguration
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required DailyConfiguration Default { get; set; }
        public DailyConfiguration? Sunday { get; set; }
        public DailyConfiguration? Monday { get; set; }
        public DailyConfiguration? Tuesday { get; set; }
        public DailyConfiguration? Wednesday { get; set; }
        public DailyConfiguration? Thursday { get; set; }
        public DailyConfiguration? Friday { get; set; }
        public DailyConfiguration? Saturday { get; set; }
    }

    public class UserRecord
    {
        public Guid Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string NameIdentifier { get; set; }
    }

    public class HeartbeatRecord
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public required TimeSpan Duration { get; set; }
        public required ScreenTime.Common.UserState UserState { get; set; }
    }

    public class ScreenTimeSummary
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public required TimeSpan TotalDuration { get; set; }
        public required TimeSpan Extensions { get; set; }
    }

    public class ExtensionRequest
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset SubmissionDate { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class ExtensionRequestResponse
    {
        public Guid Id { get; set; }
        public Guid ExtensionRequestId { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public DateTimeOffset ForDate { get; set; }
        public TimeSpan Duration { get; set; }
        public IEnumerable<ExtensionRequest>? ExtensionRequests { get; set; }
        public bool IsApproved { get; set; }
    }
}
