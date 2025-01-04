namespace ScreenTimeService.Models
{
    public class ExtensionRequest
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset SubmissionDate { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsActive { get; set; }
    }
}
