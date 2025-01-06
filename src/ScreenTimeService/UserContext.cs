using Microsoft.EntityFrameworkCore;
using ScreenTimeService;
using ScreenTimeService.Models;

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
