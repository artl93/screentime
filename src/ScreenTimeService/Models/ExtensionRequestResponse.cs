namespace ScreenTimeService.Models
{
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
