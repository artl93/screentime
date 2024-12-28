using Microsoft.EntityFrameworkCore;


public class UserEvent
{
    public required Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; } = string.Empty;
    public required DateTimeOffset DateTime { get; set; }
    public required EventKind Event { get; set; }
}

public class UserConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int DailyLimitMinutes { get; set; } = 60;
    public int WarningTimeMinutes { get; set; } = 10;
    public int WarningIntervalSeconds { get; set; } = 60;
    public int GraceMinutes { get; set; } = 5;
}

public enum EventKind
{
    Start,
    End, 
    Invalid
}


public class UserContext : DbContext
{
    public UserContext(DbContextOptions<UserContext> options) : base(options) { }

    public DbSet<UserEvent> UserEvents { get; set; }
    public DbSet<UserConfiguration> UserConfigurations { get; set; }
}
