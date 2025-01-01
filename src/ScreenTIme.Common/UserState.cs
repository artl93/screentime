using System.Text.Json.Serialization;

namespace ScreenTime.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserState { Invalid, Okay, Warn, Error, Lock, Paused }

}