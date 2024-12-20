using System.Text.Json.Serialization;

namespace ScreenTime
{
    // make it so that the properties can be lowercase when deserialized by json 
    public record UserMessage(string Title, string Message, string Icon, string Action);

}