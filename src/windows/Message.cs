using System.Text.Json.Serialization;

// make it so that the properties can be lowercase when deserialied by json 
record ServerMessage
{
    public string Title { get; set; }
    public string Message { get; set; }
    public string Icon { get; set; }
    public string Action { get; set; }
}

record UserStatus(TimeSpan LoggedInTime, string Icon, string Action, Status Status, TimeSpan dailyTimeLimit);

enum Status { Okay, Warn, Error, Lock }

record class UserConfiguration(Guid Id, string Name, int DailyLimitMinutes = 60, int WarningTimeMinutes = 10, int WarningIntervalSeconds = 60);
