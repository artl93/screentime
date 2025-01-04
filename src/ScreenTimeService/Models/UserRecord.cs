namespace ScreenTimeService.Models
{
    public class UserRecord
    {
        public Guid Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string NameIdentifier { get; set; }
    }
}
