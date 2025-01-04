using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenTime.Common
{
    public record AdminExtensionGrant(Guid UserId, TimeSpan Duration, Guid[] RequestIds);
    public record ExtensionGrant(DateTimeOffset GrantDate, TimeSpan Duration);
    public record ExtensionRequest(TimeSpan Duration);
    public record AdminExtensionRequest(Guid requestId, User User, DateTimeOffset SubmissionDate, TimeSpan Duration);
}
