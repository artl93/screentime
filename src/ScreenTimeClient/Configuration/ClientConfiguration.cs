using ScreenTime.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScreenTimeClient.Configuration
{

    public record ClientConfiguration(int WarningInterval, bool EnableOnline);
}
