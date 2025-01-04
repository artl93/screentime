namespace ScreenTimeService.Models
{
    public class ExtensionRequestResponse
    {
        public Guid Id { get; set; }
        public Guid GrantedForUserId { get; set; }
        public Guid GrantedByUserId { get; set; }
        public DateTimeOffset GratedDateTime { get; set; }
        public DateTimeOffset GrantedForDate { get; set; }
        public TimeSpan GrantedDuration { get; set; }
        public IEnumerable<ExtensionRequest>? DismissedExtensionRequests { get; set; }
    }
}
