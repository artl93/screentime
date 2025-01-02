public class UserEvent
{
    public required Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; } = string.Empty;
    public required DateTimeOffset DateTime { get; set; }
    public required EventKind Event { get; set; }
}
