using System.Text.Json.Serialization;

// make it so that the properties can be lowercase when deserialied by json 
public class ServerMessage
{
    public string Title { get; set; }
    public string Message { get; set; }
    public string Icon { get; set; }
    public string Action { get; set; }
}

record UserStatus(TimeSpan LoggedInTime, string Icon, string Action, Status Status, TimeSpan dailyTimeLimit);

enum Status { Okay, Warn, Error }