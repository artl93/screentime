using System.Text.Json.Serialization;

// make it so that the properties can be lowercase when deserialied by json 
public record UserMessage(string Title, string Message, string Icon, string Action);

public record UserStatus(TimeSpan LoggedInTime, string Icon, string Action, Status Status, TimeSpan dailyTimeLimit);

public enum Status { Okay, Warn, Error, Lock }

public record class UserConfiguration(Guid Id, string Name, int DailyLimitMinutes = 60, int WarningTimeMinutes = 10, int WarningIntervalSeconds = 60, int GraceMinutes = 5);
