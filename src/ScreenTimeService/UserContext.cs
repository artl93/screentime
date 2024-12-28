using Microsoft.EntityFrameworkCore;


public class UserContext : DbContext
{
    public UserContext(DbContextOptions<UserContext> options) : base(options) { }

    public DbSet<UserEvent> UserEvents { get; set; }
    public DbSet<UserConfiguration> UserConfigurations { get; set; }
}
